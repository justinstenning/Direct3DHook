using Capture.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Capture.Hook.Common
{
    [Serializable]
    public class ImageElement: Element
    {
        /// <summary>
        /// The image file bytes
        /// </summary>
        public virtual byte[] Image { get; set; }

        System.Drawing.Bitmap _bitmap = null;
        internal virtual System.Drawing.Bitmap Bitmap {
            get
            {
                if (_bitmap == null && Image != null)
                {
                    _bitmap = Image.ToBitmap();
                    _ownsBitmap = true;
                }

                return _bitmap;
            }
            set { _bitmap = value; }
        }

        /// <summary>
        /// This value is multiplied with the source color (e.g. White will result in same color as source image)
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="System.Drawing.Color.White"/>.
        /// </remarks>
        public virtual System.Drawing.Color Tint { get; set; } = System.Drawing.Color.White;
        
        /// <summary>
        /// The location of where to render this image element
        /// </summary>
        public virtual System.Drawing.Point Location { get; set; }

        public float Angle { get; set; }

        public float Scale { get; set; } = 1.0f;

        public string Filename { get; set; }

        bool _ownsBitmap = false;

        public ImageElement() { }

        public ImageElement(string filename):
            this(new System.Drawing.Bitmap(filename), true)
        {
            Filename = filename;
        }

        public ImageElement(System.Drawing.Bitmap bitmap, bool ownsImage = false)
        {
            Tint = System.Drawing.Color.White;
            this.Bitmap = bitmap;
            _ownsBitmap = ownsImage;
            Scale = 1.0f;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_ownsBitmap)
                {
                    SafeDispose(this.Bitmap);
                    this.Bitmap = null;
                }
            }
        }
    }
}
