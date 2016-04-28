using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Capture.Interface
{
    public static class ScreenshotExtensions
    {
        public static Bitmap ToBitmap(this byte[] data, int width, int height, int stride, PixelFormat pixelFormat)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var img = new Bitmap(width, height, stride, pixelFormat, handle.AddrOfPinnedObject());
                return img;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        public static Bitmap ToBitmap(this Screenshot screenshot)
        {
            return screenshot.Format == ImageFormat.PixelData ? screenshot.Data.ToBitmap(screenshot.Width, screenshot.Height, screenshot.Stride, screenshot.PixelFormat) 
                                                              : screenshot.Data.ToBitmap();
        }

        public static Bitmap ToBitmap(this byte[] imageBytes)
        {
            // Note: deliberately not disposing of MemoryStream, it doesn't have any unmanaged resources anyway and the GC 
            //       will deal with it. This fixes GitHub issue #19 (https://github.com/spazzarama/Direct3DHook/issues/19).
            var ms = new MemoryStream(imageBytes);
            try
            {
                var image = (Bitmap)Image.FromStream(ms);
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] ToByteArray(this Image img, System.Drawing.Imaging.ImageFormat format)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, format);
                stream.Close();
                return stream.ToArray();
            }
        }
    }
}
