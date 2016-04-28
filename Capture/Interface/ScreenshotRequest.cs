using System;
using System.Drawing;
using System.Runtime.Remoting;
using System.Security.Permissions;

namespace Capture.Interface
{
    [Serializable]
    public class ScreenshotRequest: MarshalByRefObject, IDisposable
    {
        public Guid RequestId { get; }
        public Rectangle RegionToCapture { get; }
        public Size? Resize { get; set; }
        public ImageFormat Format { get; set; }

        public ScreenshotRequest(Rectangle region, Size resize)
            : this(Guid.NewGuid(), region, resize)
        {
        }

        public ScreenshotRequest(Rectangle region)
            : this(Guid.NewGuid(), region)
        {
        }

        public ScreenshotRequest(Guid requestId, Rectangle region, Size? resize = null)
        {
            RequestId = requestId;
            RegionToCapture = region;
            Resize = resize;
        }

        public ScreenshotRequest Clone()
        {
            return new ScreenshotRequest(RequestId, RegionToCapture, Resize)
            {
                Format = Format
            };
        }

        ~ScreenshotRequest()
        {
            Dispose(false);
        }

        bool _disposed;
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
        void Disconnect()
        {
            RemotingServices.Disconnect(this);
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            // Returning null designates an infinite non-expiring lease.
            // We must therefore ensure that RemotingServices.Disconnect() is called when
            // it's no longer needed otherwise there will be a memory leak.
            return null;
        }
    }
}
