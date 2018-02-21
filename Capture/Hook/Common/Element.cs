using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text;

namespace Capture.Hook.Common
{
    [Serializable]
    public abstract class Element: IOverlayElement, IDisposable
    {
        public virtual bool Hidden { get; set; }

        ~Element()
        {
            Dispose(false);
        }

        public virtual void Frame()
        {
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">true if disposing both unmanaged and managed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        protected void SafeDispose(IDisposable disposableObj)
        {
            if (disposableObj != null)
                disposableObj.Dispose();
        }
    }
}
