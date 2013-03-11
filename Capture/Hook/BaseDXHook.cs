using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using EasyHook;
using System.IO;
using System.Runtime.Remoting;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using Capture.Interface;
using System.Threading;

namespace Capture.Hook
{
    internal abstract class BaseDXHook: IDXHook
    {
        protected readonly ClientCaptureInterfaceEventProxy InterfaceEventProxy = new ClientCaptureInterfaceEventProxy();

        public BaseDXHook(CaptureInterface ssInterface)
        {
            this.Interface = ssInterface;
            this.Timer = new Stopwatch();
            this.Timer.Start();
            this.FPS = new FramesPerSecond();

            Interface.ScreenshotRequested += InterfaceEventProxy.ScreenshotRequestedProxyHandler;
            Interface.DisplayText += InterfaceEventProxy.DisplayTextProxyHandler;
            InterfaceEventProxy.ScreenshotRequested += new ScreenshotRequestedEvent(InterfaceEventProxy_ScreenshotRequested);
            InterfaceEventProxy.DisplayText += new DisplayTextEvent(InterfaceEventProxy_DisplayText);
        }
        ~BaseDXHook()
        {
            Dispose(false);
        }

        void InterfaceEventProxy_DisplayText(DisplayTextEventArgs args)
        {
            TextDisplay = new TextDisplay()
            {
                Text = args.Text,
                Duration = args.Duration
            };
        }

        protected virtual void InterfaceEventProxy_ScreenshotRequested(ScreenshotRequest request)
        {
            
            this.Request = request;
        }

        protected Stopwatch Timer { get; set; }

        /// <summary>
        /// Frames Per second counter, FPS.Frame() must be called each frame
        /// </summary>
        protected FramesPerSecond FPS { get; set; }

        protected TextDisplay TextDisplay { get; set; }

        int _processId = 0;
        protected int ProcessId
        {
            get
            {
                if (_processId == 0)
                {
                    _processId = RemoteHooking.GetCurrentProcessId();
                }
                return _processId;
            }
        }

        protected virtual string HookName
        {
            get
            {
                return "BaseDXHook";
            }
        }

        protected void Frame()
        {
            FPS.Frame();
            if (TextDisplay != null && TextDisplay.Display) 
                TextDisplay.Frame();
        }

        protected void DebugMessage(string message)
        {
#if DEBUG
            try
            {
                Interface.Message(MessageType.Debug, HookName + ": " + message);
            }
            catch (RemotingException)
            {
                // Ignore remoting exceptions
            }
#endif
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            return GetVTblAddresses(pointer, 0, numberOfMethods);
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int startIndex, int numberOfMethods)
        {
            List<IntPtr> vtblAddresses = new List<IntPtr>();

            IntPtr vTable = Marshal.ReadIntPtr(pointer);
            for (int i = startIndex; i < startIndex + numberOfMethods; i++)
                vtblAddresses.Add(Marshal.ReadIntPtr(vTable, i * IntPtr.Size)); // using IntPtr.Size allows us to support both 32 and 64-bit processes

            return vtblAddresses.ToArray();
        }

        protected static void CopyStream(Stream input, Stream output)
        {
            int bufferSize = 32768;
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    return;
                }
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        protected static byte[] ReadFullStream(Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }
            else
            {
                byte[] buffer = new byte[32768];
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                            ms.Write(buffer, 0, read);
                        if (read < buffer.Length)
                        {
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        protected void ProcessCapture(Stream stream, Guid? requestId)
        {
            ProcessCapture(ReadFullStream(stream), requestId);
        }

        protected void ProcessCapture(byte[] bitmapData, Guid? requestId)
        {
            try
            {
                if (requestId.HasValue)
                    Interface.SendScreenshotResponse(new Screenshot(requestId.Value, bitmapData));

                LastCaptureTime = Timer.Elapsed;
            }
            catch (RemotingException)
            {
                // Ignore remoting exceptions
                // .NET Remoting will throw an exception if the host application is unreachable
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }


        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private Bitmap BitmapFromBytes(byte[] bitmapData)
        {
            using (MemoryStream ms = new MemoryStream(bitmapData))
            {
                return (Bitmap)Image.FromStream(ms);
            }
        }

        protected TimeSpan LastCaptureTime
        {
            get;
            set;
        }

        protected bool CaptureThisFrame
        {
            get
            {
                return ((Timer.Elapsed - LastCaptureTime) > CaptureDelay) || Request != null;
            }
        }
        protected TimeSpan CaptureDelay { get; set; }

        #region IDXHook Members

        public CaptureInterface Interface
        {
            get;
            set;
        }
        
        private CaptureConfig _config;
        public CaptureConfig Config
        {
            get { return _config; }
            set
            {
                _config = value;
                CaptureDelay = new TimeSpan(0, 0, 0, 0, (int)((1.0 / (double)_config.TargetFramesPerSecond) * 1000.0));
            }
        }

        private ScreenshotRequest _request;
        public ScreenshotRequest Request
        {
            get { return _request; }
            set { Interlocked.Exchange(ref _request, value);  }
        }

        protected List<LocalHook> Hooks = new List<LocalHook>();
        public abstract void Hook();

        public abstract void Cleanup();

        #endregion

        #region IDispose Implementation

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Only clean up managed objects if disposing (i.e. not called from destructor)
            if (disposing)
            {
                try
                {
                    // Uninstall Hooks
                    if (Hooks.Count > 0)
                    {
                        // First disable the hook (by excluding all threads) and wait long enough to ensure that all hooks are not active
                        foreach (var hook in Hooks)
                        {
                            // Lets ensure that no threads will be intercepted again
                            hook.ThreadACL.SetInclusiveACL(new int[] { 0 });
                        }

                        System.Threading.Thread.Sleep(100);

                        // Now we can dispose of the hooks (which triggers the removal of the hook)
                        foreach (var hook in Hooks)
                        {
                            hook.Dispose();
                        }

                        Hooks.Clear();
                    }

                    try
                    {
                        // Remove the event handlers
                        Interface.ScreenshotRequested -= InterfaceEventProxy.ScreenshotRequestedProxyHandler;
                        Interface.DisplayText -= InterfaceEventProxy.DisplayTextProxyHandler;
                    }
                    catch (RemotingException) { } // Ignore remoting exceptions (host process may have been closed)
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}
