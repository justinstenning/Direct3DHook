using System.Drawing;

namespace Capture.Hook.Common
{
    public class ImageElement: Element
    {
        public virtual Bitmap Bitmap { get; set; }
        
        /// <summary>
        /// This value is multiplied with the source color (e.g. White will result in same color as source image)
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Color.White"/>.
        /// </remarks>
        public virtual Color Tint { get; }
        
        /// <summary>
        /// The location of where to render this image element
        /// </summary>
        public virtual Point Location { get; set; }

        public float Angle { get; set; }

        public float Scale { get; }

        public string Filename { get; }

        readonly bool _ownsBitmap;

        public ImageElement(string filename):
            this(new Bitmap(filename), true)
        {
            Filename = filename;
        }

        public ImageElement(Bitmap bitmap, bool ownsImage = false)
        {
            Tint = Color.White;
            Bitmap = bitmap;
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
                    SafeDispose(Bitmap);
                    Bitmap = null;
                }
            }
        }
    }
}
