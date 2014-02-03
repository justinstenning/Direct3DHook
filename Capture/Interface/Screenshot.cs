using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Permissions;

namespace Capture.Interface
{
    public class Screenshot : MarshalByRefObject, IDisposable
    {
        private bool _disposed;

        public Screenshot(Guid requestId, byte[] capturedBitmap)
        {
            _requestId = requestId;
            _capturedBitmap = capturedBitmap;
        }

        ~Screenshot()
        {
            Dispose(false);
        }

        Guid _requestId;
        public Guid RequestId
        {
            get
            {
                return _requestId;
            }
        }

        byte[] _capturedBitmap;
        public byte[] CapturedBitmap
        {
            get
            {
                return _capturedBitmap;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Disconnects the remoting channel(s) of this object and all nested objects.
        /// </summary>
        private void Disconnect()
        {
            RemotingServices.Disconnect(this);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            // Returning null designates an infinite non-expiring lease.
            // We must therefore ensure that RemotingServices.Disconnect() is called when
            // it's no longer needed otherwise there will be a memory leak.
            return null;
        }
    }

    public static class BitmapExtension
    {
        public static Bitmap ToBitmap(this byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                try
                {
                    Bitmap image = (Bitmap)Image.FromStream(ms);
                    return image;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
