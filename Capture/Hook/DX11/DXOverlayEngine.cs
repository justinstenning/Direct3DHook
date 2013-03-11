using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Capture.Hook.Common;
using SharpDX.Direct3D11;
using SharpDX;
using System.Diagnostics;

namespace Capture.Hook.DX11
{
    internal class DXOverlayEngine: DisposeBase
    {
        public List<IOverlay> Overlays { get; set; }
        public bool DeferredContext
        {
            get
            {
                return _deviceContext.TypeInfo == DeviceContextType.Deferred;
            }
        }

        bool _initialised = false;
        bool _initialising = false;

        Device _device;
        DeviceContext _deviceContext;
        Texture2D _renderTarget;
        RenderTargetView _renderTargetView;
        DXSprite _spriteEngine;
        Dictionary<string, DXFont> _fontCache = new Dictionary<string, DXFont>();

        public DXOverlayEngine()
        {
            Overlays = new List<IOverlay>();
        }

        private void EnsureInitiliased()
        {
            Debug.Assert(_initialised);
        }

        public bool Initialise(SharpDX.DXGI.SwapChain swapChain)
        {
            return Initialise(swapChain.GetDevice<Device>(), swapChain.GetBackBuffer<Texture2D>(0));
        }

        public bool Initialise(Device device, Texture2D renderTarget)
        {
            Debug.Assert(!_initialised);
            if (_initialising)
                return false;

            _initialising = true;
            
            try
            {

                _device = device;
                _renderTarget = renderTarget;
                try
                {
                    // TODO: determine if any benefit to using deferred context here
                    _deviceContext = new DeviceContext(_device);
                }
                catch (SharpDXException)
                {
                    _deviceContext = _device.ImmediateContext;
                }

                _renderTargetView = new RenderTargetView(_device, _renderTarget);

                if (DeferredContext)
                {
                    _deviceContext.Rasterizer.SetViewports(new ViewportF(0, 0, _renderTarget.Description.Width, _renderTarget.Description.Height, 0, 1));
                    _deviceContext.OutputMerger.SetTargets(_renderTargetView);
                }

                _spriteEngine = new DXSprite(_device, _deviceContext);
                if (!_spriteEngine.Initialize())
                    return false;

                // Initialise any resources required for overlay elements
                IntialiseElementResources();

                _initialised = true;
                return true;
            }
            finally
            {
                _initialising = false;
            }
        }

        private void IntialiseElementResources()
        {
            foreach (var overlay in Overlays)
            {
                foreach (var element in overlay.Elements)
                {
                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        GetFontForTextElement(textElement);
                    }
                    else if (imageElement != null)
                    {
                        // TODO: load image element into texture
                    }
                }
            }
        }

        private void Begin()
        {
            if (!DeferredContext)
            {
                _deviceContext.Rasterizer.SetViewports(new ViewportF(0, 0, _renderTarget.Description.Width, _renderTarget.Description.Height, 0, 1));
                _deviceContext.OutputMerger.SetTargets(_renderTargetView);
            }
        }

        /// <summary>
        /// Draw the overlay(s)
        /// </summary>
        public void Draw()
        {
            EnsureInitiliased();

            Begin();

            foreach (var overlay in Overlays)
            {
                foreach (var element in overlay.Elements)
                {
                    var textElement = element as TextElement;
                    var imageElement = element as ImageElement;

                    if (textElement != null)
                    {
                        DXFont font = GetFontForTextElement(textElement);
                        if (font != null && !String.IsNullOrEmpty(textElement.Text))
                            _spriteEngine.DrawString(textElement.Location.X, textElement.Location.Y, textElement.Text, textElement.Color.R, textElement.Color.G, textElement.Color.B, textElement.Color.A, font);
                    }
                    else if (imageElement != null)
                    {
                    }
                }
            }

            End();
        }

        private void End()
        {
            if (DeferredContext)
            {
                var commandList = _deviceContext.FinishCommandList(true);
                _device.ImmediateContext.ExecuteCommandList(commandList, true);
                commandList.Dispose();
            }
        }

        DXFont GetFontForTextElement(TextElement element)
        {
            DXFont result = null;

            string fontKey = String.Format("{0}{1}{2}", element.Font.Name, element.Font.Size, element.Font.Style, element.AntiAliased);

            if (!_fontCache.TryGetValue(fontKey, out result))
            {
                result = new DXFont(_device, _deviceContext);
                result.Initialize(element.Font.Name, element.Font.Size, element.Font.Style, element.AntiAliased);
                _fontCache[fontKey] = result;
            }
            return result;
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources
        /// </summary>
        /// <param name="disposing">true if disposing both unmanaged and managed</param>
        protected override void Dispose(bool disposing)
        {
            if (true)
            {
                SafeDispose(_device);
                _device = null;
            }
        }

        void SafeDispose(DisposeBase disposableObj)
        {
            if (disposableObj != null)
                disposableObj.Dispose();
        }
    }
}
