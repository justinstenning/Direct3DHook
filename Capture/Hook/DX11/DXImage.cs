using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Rectangle = System.Drawing.Rectangle;

namespace Capture.Hook.DX11
{
    public class DXImage : Component
    {
        DeviceContext _deviceContext;
        Texture2D _tex;
        ShaderResourceView _texSRV;
        bool _initialised;

        public int Width { get; set; }

        public int Height { get; set; }

        public Device Device { get; }

        public DXImage(Device device, DeviceContext deviceContext): base("DXImage")
        {
            Device = device;
            _deviceContext = deviceContext;
            _tex = null;
            _texSRV = null;
            Width = 0;
            Height = 0;
        }

        public bool Initialise(Bitmap bitmap)
        {
            RemoveAndDispose(ref _tex);
            RemoveAndDispose(ref _texSRV);

            //Debug.Assert(bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Width = bitmap.Width;
            Height = bitmap.Height;

            var bmData = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var texDesc = new Texture2DDescription
                {
                    Width = Width,
                    Height = Height,
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
                data.RowPitch = bmData.Stride;// _texWidth * 4;
                data.SlicePitch = 0;

                _tex = ToDispose(new Texture2D(Device, texDesc, new[] { data }));
                if (_tex == null)
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

                _texSRV = ToDispose(new ShaderResourceView(Device, _tex, srvDesc));
                if (_texSRV == null)
                    return false;
            }
            finally
            {
                bitmap.UnlockBits(bmData);
            }

            _initialised = true;

            return true;
        }

        public ShaderResourceView GetSRV()
        {
            Debug.Assert(_initialised);
            return _texSRV;
        }
    }
}
