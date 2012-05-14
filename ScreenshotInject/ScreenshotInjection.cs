using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyHook;
using ScreenshotInterface;
using System.Runtime.InteropServices;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using System.Threading;
using System.IO;
using System.Reflection;

namespace ScreenshotInject
{
    public class ScreenshotInjection : EasyHook.IEntryPoint
    {
        IDXHook _directXHook = null;

        private ScreenshotInterface.ScreenshotInterface _interface = null;
        
        public ScreenshotInjection(
            RemoteHooking.IContext context,
            String channelName,
            String version,
            bool showOverlay)
        {
            // Get reference to IPC to host application
            // Note: any methods called or events triggered against _interface will execute in the host process.
            _interface = RemoteHooking.IpcConnectClient<ScreenshotInterface.ScreenshotInterface>(channelName);
        }

        /// <summary>
        /// Called by EasyHook to begin any hooking etc in the target process
        /// </summary>
        /// <param name="InContext"></param>
        /// <param name="channelName"></param>
        /// <param name="strVersion">Direct3DVersion passed as a string so that GAC registration is not required</param>
        /// <param name="showOverlay">Whether or not to show an overlay</param>
        public void Run(
            RemoteHooking.IContext InContext,
            String channelName,
            String strVersion,
            bool showOverlay)
        {
            Direct3DVersion version = (Direct3DVersion)Enum.Parse(typeof(Direct3DVersion), strVersion);
            // NOTE: We are now already running within the target process
            try
            {
                _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "DLL Injection succeeded");

                bool isX64Process = RemoteHooking.IsX64Process(RemoteHooking.GetCurrentProcessId());
                _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "64-bit Process: " + isX64Process.ToString());

                if (version == Direct3DVersion.AutoDetect)
                {
                    // Attempt to determine the correct version based on loaded module.
                    // In most cases this will work fine, however it is perfectly ok for an application to use a D3D10 device along with D3D11 devices
                    // so the version might matched might not be the one you want to use
                    IntPtr d3D9Loaded = IntPtr.Zero;
                    IntPtr d3D10Loaded = IntPtr.Zero;
                    IntPtr d3D10_1Loaded = IntPtr.Zero;
                    IntPtr d3D11Loaded = IntPtr.Zero;
                    IntPtr d3D11_1Loaded = IntPtr.Zero;

                    int delayTime = 100;
                    int retryCount = 0;
                    while (d3D9Loaded == IntPtr.Zero && d3D10Loaded == IntPtr.Zero && d3D10_1Loaded == IntPtr.Zero && d3D11Loaded == IntPtr.Zero && d3D11_1Loaded == IntPtr.Zero)
                    {
                        retryCount++;
                        d3D9Loaded = GetModuleHandle("d3d9.dll");
                        d3D10Loaded = GetModuleHandle("d3d10.dll");
                        d3D10_1Loaded = GetModuleHandle("d3d10_1.dll");
                        d3D11Loaded = GetModuleHandle("d3d11.dll");
                        d3D11_1Loaded = GetModuleHandle("d3d11_1.dll");
                        Thread.Sleep(delayTime);

                        if (retryCount * delayTime > 5000)
                        {
                            _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Unsupported Direct3DVersion, or Direct3D DLL not loaded within 5 seconds.");
                            return;
                        }
                    }

                    version = Direct3DVersion.Unknown;
                    if (d3D11_1Loaded != IntPtr.Zero)
                    {
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Autodetect found Direct3D 11.1");
                        version = Direct3DVersion.Direct3D11_1;
                    }
                    else if (d3D11Loaded != IntPtr.Zero)
                    {
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Autodetect found Direct3D 11");
                        version = Direct3DVersion.Direct3D11;
                    }
                    else if (d3D10_1Loaded != IntPtr.Zero)
                    {
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Autodetect found Direct3D 10.1");
                        version = Direct3DVersion.Direct3D10_1;
                    }
                    else if (d3D10Loaded != IntPtr.Zero)
                    {
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Autodetect found Direct3D 10");
                        version = Direct3DVersion.Direct3D10;
                    }
                    else if (d3D9Loaded != IntPtr.Zero)
                    {
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Autodetect found Direct3D 9");
                        version = Direct3DVersion.Direct3D9;
                    }
                }

                switch (version)
                {
                    case Direct3DVersion.Direct3D9:
                        _directXHook = new DXHookD3D9(_interface);
                        break;
                    case Direct3DVersion.Direct3D10:
                        _directXHook = new DXHookD3D10(_interface);
                        break;
                    case Direct3DVersion.Direct3D10_1:
                        _directXHook = new DXHookD3D10_1(_interface);
                        break;
                    case Direct3DVersion.Direct3D11:
                        _directXHook = new DXHookD3D11(_interface);
                        break;
                    default:
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Unsupported Direct3DVersion");
                        break;
                }
                _directXHook.ShowOverlay = showOverlay;

                _directXHook.Hook();
            }
            catch (Exception e)
            {
                /*
                    We should notify our host process about this error...
                 */
                //_interface.ReportError(RemoteHooking.GetCurrentProcessId(), e);
                _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(),"Exception during device creation and hooking: \r\n" + e.Message);
                while (_interface.Ping(RemoteHooking.GetCurrentProcessId()))
                {
                    Thread.Sleep(100);
                }

                return;
            }

            // Wait for host process termination...
            try
            {
                // When not using GAC there can be issues with remoting assemblies resolving correctly
                // this is a workaround that ensures that the current assembly is correctly associated
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += (sender, args) =>
                {
                    return this.GetType().Assembly.FullName == args.Name ? this.GetType().Assembly : null;
                };

                while (_interface.Ping(RemoteHooking.GetCurrentProcessId()))
                {
                    Thread.Sleep(10);

                    ScreenshotRequest request = _interface.GetScreenshotRequest(RemoteHooking.GetCurrentProcessId());

                    if (request != null)
                    {
                        _directXHook.Request = request;
                    }
                }
            }
            catch
            {
                // .NET Remoting will raise an exception if host is unreachable
            }
            finally
            {
                try
                {
                    _directXHook.Cleanup();
                }
                catch
                {
                }
            }
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}
