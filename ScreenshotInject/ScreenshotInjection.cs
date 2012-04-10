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
            Direct3DVersion version)
        {
            // Get reference to IPC to host application
            // Note: any methods called or events triggered against _interface will execute in the host process.
            _interface = RemoteHooking.IpcConnectClient<ScreenshotInterface.ScreenshotInterface>(channelName);
        }

        /// <summary>
        /// Called by EasyHook to begin any hooking etc in the target process
        /// </summary>
        /// <param name="InContext"></param>
        /// <param name="InArg1"></param>
        public void Run(
                    RemoteHooking.IContext InContext,
                    String channelName,
                    Direct3DVersion version)
        {
            // NOTE: We are now already running within the target process
            try
            {
                _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "DLL Injection succeeded");

                bool isX64Process = RemoteHooking.IsX64Process(RemoteHooking.GetCurrentProcessId());
                _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "64-bit Process: " + isX64Process.ToString());

                switch (version)
                {
                    case Direct3DVersion.Direct3D9:
                        _directXHook = new DXHookD3D9(_interface);
                        break;
                    case Direct3DVersion.Direct3D10:
                        _directXHook = new DXHookD3D10(_interface);
                        break;
                    //case Direct3DVersion.Direct3D10_1:
                    //    _directXHook = new DXHookD3D10(_interface);
                    //    break;
                    case Direct3DVersion.Direct3D11:
                        _directXHook = new DXHookD3D11(_interface);
                        break;
                    default:
                        _interface.OnDebugMessage(RemoteHooking.GetCurrentProcessId(), "Unsupported Direct3DVersion");
                        break;

                }

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
