using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace Capture.Interface
{
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Lifetime;
    using System.Security.Permissions;

    public class Screenshot : MarshalByRefObject, IDisposable
    {
        private bool _disposed;

        public Screenshot(Guid requestId, byte[] capturedBitmap)
        {
            _requestId = requestId;
            _capturedBitmap = capturedBitmap;
        }

        public Screenshot(Guid requestId, byte[] capturedBitmap, int width, int height, int pitch)
        {
            this.Width = width;
            this.Height = height;
            this.Pitch = pitch;
            _requestId = requestId;
            _capturedBitmap = capturedBitmap;
        }

        ~Screenshot()
        {
            Dispose(false);
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
            //ILease lease = (ILease)base.InitializeLifetimeService();
            //if (lease.CurrentState == LeaseState.Initial)
            //{
            //    lease.InitialLeaseTime = TimeSpan.FromSeconds(10);
            //    lease.SponsorshipTimeout = TimeSpan.FromSeconds(10);
            //    lease.RenewOnCallTime = TimeSpan.FromSeconds(10);
            //}

            //return lease;            
            //
            // Returning null designates an infinite non-expiring lease.
            // We must therefore ensure that RemotingServices.Disconnect() is called when
            // it's no longer needed otherwise there will be a memory leak.
            //
            return null;

            //var lease = (ILease)base.InitializeLifetimeService();
            //if (lease.CurrentState == LeaseState.Initial)
            //{
            //    lease.InitialLeaseTime = TimeSpan.FromSeconds(2);
            //    lease.SponsorshipTimeout = TimeSpan.FromSeconds(5);
            //    lease.RenewOnCallTime = TimeSpan.FromSeconds(2);
            //}
            //return lease;
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

        public int Height { get; protected set; }

        public int Width { get; protected set; }

        public int Pitch { get; protected set; }

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
