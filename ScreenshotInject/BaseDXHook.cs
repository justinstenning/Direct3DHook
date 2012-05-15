using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using EasyHook;
using System.IO;
using System.Runtime.Remoting;

namespace ScreenshotInject
{
    internal abstract class BaseDXHook: IDXHook
    {
        public BaseDXHook(ScreenshotInterface.ScreenshotInterface ssInterface)
        {
            this.Interface = ssInterface;
        }

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

        protected void DebugMessage(string message)
        {
#if DEBUG
            try
            {
                Interface.OnDebugMessage(this.ProcessId, HookName + ": " + message);
            }
            catch (RemotingException re)
            {
                // Ignore remoting exceptions
            }
#endif
        }

        protected IntPtr[] GetVTblAddresses(IntPtr pointer, int numberOfMethods)
        {
            List<IntPtr> vtblAddresses = new List<IntPtr>();

            IntPtr vTable = Marshal.ReadIntPtr(pointer);
            for (int i = 0; i < numberOfMethods; i++)
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
                stream.Position = 0;
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
                            return ms.ToArray();
                    }
                }
            }
        }

        protected void SendResponse(Stream stream, Guid requestId)
        {
            SendResponse(ReadFullStream(stream), requestId);
        }

        protected void SendResponse(byte[] bitmapData, Guid requestId)
        {
            try
            {
                // Send the buffer back to the host process
                Interface.OnScreenshotResponse(RemoteHooking.GetCurrentProcessId(), requestId, bitmapData);
            }
            catch (RemotingException re)
            {
                // Ignore remoting exceptions
                // .NET Remoting will throw an exception if the host application is unreachable
            }
        }


        #region IDXHook Members

        public ScreenshotInterface.ScreenshotInterface Interface
        {
            get;
            set;
        }

        public bool ShowOverlay
        {
            get;
            set;
        }

        private ScreenshotInterface.ScreenshotRequest _request;
        public ScreenshotInterface.ScreenshotRequest Request
        {
            get { return _request; }
            set { _request = value;  }
        }

        public abstract void Hook();

        public abstract void Cleanup();

        #endregion
    }
}
