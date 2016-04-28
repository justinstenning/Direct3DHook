// Adapted from Frank Luna's "Sprites and Text" example here: http://www.d3dcoder.net/resources.htm 
// checkout his books here: http://www.d3dcoder.net/default.htm

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Color = System.Drawing.Color;
using Device = SharpDX.Direct3D11.Device;
using Rectangle = SharpDX.Rectangle;

namespace Capture.Hook.DX11
{
    public class DXFont : IDisposable
    {
        Device _device;
        DeviceContext _deviceContext;

        public DXFont(Device device, DeviceContext deviceContext)
        {
            _device = device;
            _deviceContext = deviceContext;
            _initialized = false;
            _fontSheetTex = null;
            _fontSheetSRV = null;
            _texWidth = 1024;
            _texHeight = 0;
            _spaceWidth = 0;
            _charHeight = 0;
        }

        public void Dispose()
        {
            _fontSheetTex?.Dispose();
            _fontSheetSRV?.Dispose();

            _fontSheetTex = null;
            _fontSheetSRV = null;
            _device = null;
            _deviceContext = null;
        }

        enum STYLE
        {
            STYLE_NORMAL = 0,
            STYLE_BOLD = 1,
            STYLE_ITALIC = 2,
            STYLE_BOLD_ITALIC = 3,
            STYLE_UNDERLINE = 4,
            STYLE_STRIKEOUT = 8
        }

        bool _initialized;
        const char StartChar = (char)33;
        const char EndChar = (char)127;
        const uint NumChars = EndChar - StartChar;
        ShaderResourceView _fontSheetSRV;
        Texture2D _fontSheetTex;
        readonly int _texWidth;
        int _texHeight;
        readonly Rectangle[] _charRects = new Rectangle[NumChars];
        int _spaceWidth, _charHeight;

        public bool Initialize(string FontName, float FontSize, FontStyle FontStyle, bool AntiAliased)
        {
            Debug.Assert(!_initialized);
            var font = new Font(FontName, FontSize, FontStyle, GraphicsUnit.Pixel);

            var hint = AntiAliased ? TextRenderingHint.AntiAlias : TextRenderingHint.SystemDefault;

            var tempSize = (int)(FontSize * 2);
            using (var charBitmap = new Bitmap(tempSize, tempSize, PixelFormat.Format32bppArgb))
            {
                using (var charGraphics = Graphics.FromImage(charBitmap))
                {
                    charGraphics.PageUnit = GraphicsUnit.Pixel;
                    charGraphics.TextRenderingHint = hint;

                    MeasureChars(font, charGraphics);

                    using (var fontSheetBitmap = new Bitmap(_texWidth, _texHeight, PixelFormat.Format32bppArgb))
                    {
                        using (var fontSheetGraphics = Graphics.FromImage(fontSheetBitmap))
                        {
                            fontSheetGraphics.CompositingMode = CompositingMode.SourceCopy;
                            fontSheetGraphics.Clear(Color.FromArgb(0, Color.Black));

                            BuildFontSheetBitmap(font, charGraphics, charBitmap, fontSheetGraphics);

                            if (!BuildFontSheetTexture(fontSheetBitmap))
                            {
                                return false;
                            }
                        }
                        //System.Drawing.Bitmap bm = new System.Drawing.Bitmap(fontSheetBitmap);
                        //bm.Save(@"C:\temp\test.png");
                    }
                }
            }

            _initialized = true;

            return true;
        }

        bool BuildFontSheetTexture(Bitmap fontSheetBitmap)
        {
            var bmData = fontSheetBitmap.LockBits(new System.Drawing.Rectangle(0, 0, _texWidth, _texHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            var texDesc = new Texture2DDescription
            {
                Width = _texWidth,
                Height = _texHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };


            DataBox data;
            data.DataPointer = bmData.Scan0;
            data.RowPitch = _texWidth * 4;
            data.SlicePitch = 0;

            _fontSheetTex = new Texture2D(_device, texDesc, new[] { data });
            if (_fontSheetTex == null)
                return false;

            var srvDesc = new ShaderResourceViewDescription
            {
                Format = Format.B8G8R8A8_UNorm,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D =
                {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };

            _fontSheetSRV = new ShaderResourceView(_device, _fontSheetTex, srvDesc);
            if (_fontSheetSRV == null)
                return false;

            fontSheetBitmap.UnlockBits(bmData);

            return true;
        }

        void MeasureChars(Font font, Graphics charGraphics)
        {
            var allChars = new char[NumChars];

            for (var i = (char)0; i < NumChars; ++i)
                allChars[i] = (char)(StartChar + i);

            var size = charGraphics.MeasureString(new string(allChars), font, new PointF(0, 0), StringFormat.GenericDefault);

            _charHeight = (int)(size.Height + 0.5f);

            var numRows = (int)(size.Width / _texWidth) + 1;
            _texHeight = numRows * _charHeight + 1;

            var sf = StringFormat.GenericDefault;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            size = charGraphics.MeasureString(" ", font, 0, sf);
            _spaceWidth = (int)(size.Width + 0.5f);
        }

        void BuildFontSheetBitmap(Font font, Graphics charGraphics, Bitmap charBitmap, Graphics fontSheetGraphics)
        {
            var whiteBrush = Brushes.White;
            var fontSheetX = 0;
            var fontSheetY = 0;


            for (var i = 0; i < NumChars; ++i)
            {
                charGraphics.Clear(Color.FromArgb(0, Color.Black));
                charGraphics.DrawString(((char)(StartChar + i)).ToString(), font, whiteBrush, new PointF(0.0f, 0.0f));

                var minX = GetCharMinX(charBitmap);
                var maxX = GetCharMaxX(charBitmap);
                var charWidth = maxX - minX + 1;

                if (fontSheetX + charWidth >= _texWidth)
                {
                    fontSheetX = 0;
                    fontSheetY += _charHeight + 1;
                }

                _charRects[i] = new Rectangle(fontSheetX, fontSheetY, charWidth, _charHeight);

                fontSheetGraphics.DrawImage(charBitmap, fontSheetX, fontSheetY, new System.Drawing.Rectangle(minX, 0, charWidth, _charHeight), GraphicsUnit.Pixel);

                fontSheetX += charWidth + 1;
            }
        }

        static int GetCharMaxX(Bitmap charBitmap)
        {
            var width = charBitmap.Width;
            var height = charBitmap.Height;

            for (var x = width - 1; x >= 0; --x)
            {
                for (var y = 0; y < height; ++y)
                {
                    var color = charBitmap.GetPixel(x, y);
                    if (color.A > 0)
                        return x;
                }
            }

            return width - 1;
        }

        static int GetCharMinX(Bitmap charBitmap)
        {
            var width = charBitmap.Width;
            var height = charBitmap.Height;

            for (var x = 0; x < width; ++x)
            {
                for (var y = 0; y < height; ++y)
                {
                    var color = charBitmap.GetPixel(x, y);
                    if (color.A > 0)
                        return x;
                }
            }

            return 0;
        }

        public ShaderResourceView GetFontSheetSRV()
        {
            Debug.Assert(_initialized);

            return _fontSheetSRV;
        }

        public Rectangle GetCharRect(char c)
        {
            Debug.Assert(_initialized);

            return _charRects[c - StartChar];
        }

        public int GetSpaceWidth()
        {
            Debug.Assert(_initialized);

            return _spaceWidth;
        }

        public int GetCharHeight()
        {
            Debug.Assert(_initialized);

            return _charHeight;
        }

    }
}


