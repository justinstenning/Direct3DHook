using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace Capture.Interface
{
    public class Screenshot : MarshalByRefObject
    {
        public Screenshot(Guid requestId, byte[] capturedBitmap)
        {
            _requestId = requestId;
            _capturedBitmap = capturedBitmap;
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
