using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX;
using System.Runtime.InteropServices;
using EasyHook;
using System.Threading;
using System.IO;

namespace ScreenshotInject
{
    enum D3D11DeviceVTbl : short
    {
        // IUnknown
        QueryInterface = 0,
        AddRef = 1,
        Release = 2,

        // ID3D11Device
        CreateBuffer = 3,
        CreateTexture1D = 4,
        CreateTexture2D = 5,
        CreateTexture3D = 6,
        CreateShaderResourceView = 7,
        CreateUnorderedAccessView = 8,
        CreateRenderTargetView = 9,
        CreateDepthStencilView = 10,
        CreateInputLayout = 11,
        CreateVertexShader = 12,
        CreateGeometryShader = 13,
        CreateGeometryShaderWithStreamOutput = 14,
        CreatePixelShader = 15,
        CreateHullShader = 16,
        CreateDomainShader = 17,
        CreateComputeShader = 18,
        CreateClassLinkage = 19,
        CreateBlendState = 20,
        CreateDepthStencilState = 21,
        CreateRasterizerState = 22,
        CreateSamplerState = 23,
        CreateQuery = 24,
        CreatePredicate = 25,
        CreateCounter = 26,
        CreateDeferredContext = 27,
        OpenSharedResource = 28,
        CheckFormatSupport = 29,
        CheckMultisampleQualityLevels = 30,
        CheckCounterInfo = 31,
        CheckCounter = 32,
        CheckFeatureSupport = 33,
        GetPrivateData = 34,
        SetPrivateData = 35,
        SetPrivateDataInterface = 36,
        GetFeatureLevel = 37,
        GetCreationFlags = 38,
        GetDeviceRemovedReason = 39,
        GetImmediateContext = 40,
        SetExceptionMode = 41,
        GetExceptionMode = 42,
    }

    /// <summary>
    /// Direct3D 11 Hook - this hooks the SwapChain.Present to take screenshots
    /// </summary>
    internal class DXHookD3D11: BaseDXHook
    {
        const int D3D11_DEVICE_METHOD_COUNT = 43;

        public DXHookD3D11(ScreenshotInterface.ScreenshotInterface ssInterface)
            : base(ssInterface)
        {
        }

        List<IntPtr> _d3d11VTblAddresses = null;
        List<IntPtr> _dxgiSwapChainVTblAddresses = null;

        LocalHook DXGISwapChain_PresentHook = null;
        LocalHook DXGISwapChain_ResizeTargetHook = null;

        protected override string HookName
        {
            get
            {
                return "DXHookD3D11";
            }
        }

        public override void Hook()
        {
            if (_d3d11VTblAddresses == null)
            {
                _d3d11VTblAddresses = new List<IntPtr>();
                _dxgiSwapChainVTblAddresses = new List<IntPtr>();

                #region Get Device and SwapChain method addresses
                // Create temporary device + swapchain and determine method addresses
                SlimDX.Direct3D11.Device device;
                SwapChain swapChain;
                using (SlimDX.Windows.RenderForm renderForm = new SlimDX.Windows.RenderForm())
                {
                    SlimDX.Result result = SlimDX.Direct3D11.Device.CreateWithSwapChain(
                        DriverType.Hardware,
                        DeviceCreationFlags.None,
                        DXGI.CreateSwapChainDescription(renderForm.Handle),
                        out device,
                        out swapChain);

                    if (result.IsSuccess)
                    {
                        this.DebugMessage("Hook: Device created");
                        using (device)
                        {
                            _d3d11VTblAddresses.AddRange(GetVTblAddresses(device.ComPointer, D3D11_DEVICE_METHOD_COUNT));

                            using (swapChain)
                            {
                                _dxgiSwapChainVTblAddresses.AddRange(GetVTblAddresses(swapChain.ComPointer, DXGI.DXGI_SWAPCHAIN_METHOD_COUNT));
                            }
                        }
                    }
                    else
                    {
                        this.DebugMessage("Hook: Device creation failed");
                    }
                }
                #endregion
            }

            // We will capture the backbuffer here
            DXGISwapChain_PresentHook = LocalHook.Create(
                _dxgiSwapChainVTblAddresses[(int)DXGI.DXGISwapChainVTbl.Present],
                new DXGISwapChain_PresentDelegate(PresentHook),
                this);
            
            // We will capture target/window resizes here
            DXGISwapChain_ResizeTargetHook = LocalHook.Create(
                _dxgiSwapChainVTblAddresses[(int)DXGI.DXGISwapChainVTbl.ResizeTarget],
                new DXGISwapChain_ResizeTargetDelegate(ResizeTargetHook),
                this);

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */
            DXGISwapChain_PresentHook.ThreadACL.SetExclusiveACL(new Int32[1]);

            DXGISwapChain_ResizeTargetHook.ThreadACL.SetExclusiveACL(new Int32[1]);
        }

        public override void Cleanup()
        {
            try
            {
                DXGISwapChain_PresentHook.Dispose();
                DXGISwapChain_ResizeTargetHook.Dispose();
                this.Request = null;
            }
            catch
            {
            }
        }

        /// <summary>
        /// The IDXGISwapChain.Present function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int DXGISwapChain_PresentDelegate(IntPtr swapChainPtr, int syncInterval, /* int */ SlimDX.DXGI.PresentFlags flags);

        /// <summary>
        /// The IDXGISwapChain.ResizeTarget function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int DXGISwapChain_ResizeTargetDelegate(IntPtr swapChainPtr, ref DXGI.DXGI_MODE_DESC newTargetParameters);

        /// <summary>
        /// Hooked to allow resizing a texture/surface that is reused. Currently not in use as we create the texture for each request
        /// to support different sizes each time (as we use DirectX to copy only the region we are after rather than the entire backbuffer)
        /// </summary>
        /// <param name="swapChainPtr"></param>
        /// <param name="newTargetParameters"></param>
        /// <returns></returns>
        int ResizeTargetHook(IntPtr swapChainPtr, ref DXGI.DXGI_MODE_DESC newTargetParameters)
        {
            using (SlimDX.DXGI.SwapChain swapChain = SlimDX.DXGI.SwapChain.FromPointer(swapChainPtr))
            {
                // This version creates a new texture for each request so there is nothing to resize.
                // IF the size of the texture is known each time, we could create it once, and then possibly need to resize it here

                return swapChain.ResizeTarget(
                    new SlimDX.DXGI.ModeDescription()
                    {
                        Format = newTargetParameters.Format,
                        Height = newTargetParameters.Height,
                        RefreshRate = newTargetParameters.RefreshRate,
                        Scaling = newTargetParameters.Scaling,
                        ScanlineOrdering = newTargetParameters.ScanlineOrdering,
                        Width = newTargetParameters.Width
                    }
                ).Code;
            }
        }

        private SwapChain _swapChain;
        private IntPtr _swapChainPointer;


        /// <summary>
        /// Our present hook that will grab a copy of the backbuffer when requested. Note: this supports multi-sampling (anti-aliasing)
        /// </summary>
        /// <param name="swapChainPtr"></param>
        /// <param name="syncInterval"></param>
        /// <param name="flags"></param>
        /// <returns>The HRESULT of the original method</returns>
        int PresentHook(IntPtr swapChainPtr, int syncInterval, SlimDX.DXGI.PresentFlags flags)
        {
            if (swapChainPtr != _swapChainPointer)
            {
                _swapChain = SlimDX.DXGI.SwapChain.FromPointer(swapChainPtr);
            }
            SwapChain swapChain = _swapChain;

            //using (SlimDX.DXGI.SwapChain swapChain = SlimDX.DXGI.SwapChain.FromPointer(swapChainPtr))
            {
                try
                {
                    #region Screenshot Request
                    if (this.Request != null)
                    {
                        this.DebugMessage("PresentHook: Request Start");
                        DateTime startTime = DateTime.Now;
                        using (Texture2D texture = Texture2D.FromSwapChain<Texture2D>(swapChain, 0))
                        {
                            #region Determine region to capture
                            System.Drawing.Rectangle regionToCapture = new System.Drawing.Rectangle(0, 0, texture.Description.Width, texture.Description.Height);

                            if (this.Request.RegionToCapture.Width > 0)
                            {
                                regionToCapture = this.Request.RegionToCapture;
                            }
                            #endregion

                            var theTexture = texture;

                            // If texture is multisampled, then we can use ResolveSubresource to copy it into a non-multisampled texture
                            Texture2D textureResolved = null;
                            if (texture.Description.SampleDescription.Count > 1)
                            {
                                this.DebugMessage("PresentHook: resolving multi-sampled texture");
                                // texture is multi-sampled, lets resolve it down to single sample
                                textureResolved = new Texture2D(texture.Device, new Texture2DDescription()
                                {
                                    CpuAccessFlags = CpuAccessFlags.None,
                                    Format = texture.Description.Format,
                                    Height = texture.Description.Height,
                                    Usage = ResourceUsage.Default,
                                    Width = texture.Description.Width,
                                    ArraySize = 1,
                                    SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0), // Ensure single sample
                                    BindFlags = BindFlags.None,
                                    MipLevels = 1,
                                    OptionFlags = texture.Description.OptionFlags
                                });
                                // Resolve into textureResolved
                                texture.Device.ImmediateContext.ResolveSubresource(texture, 0, textureResolved, 0, texture.Description.Format);

                                // Make "theTexture" be the resolved texture
                                theTexture = textureResolved;
                            }

                            // Create destination texture
                            Texture2D textureDest = new Texture2D(texture.Device, new Texture2DDescription()
                            {
                                CpuAccessFlags = CpuAccessFlags.None,// CpuAccessFlags.Write | CpuAccessFlags.Read,
                                Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm, // Supports BMP/PNG
                                Height = regionToCapture.Height,
                                Usage = ResourceUsage.Default,// ResourceUsage.Staging,
                                Width = regionToCapture.Width,
                                ArraySize = 1,//texture.Description.ArraySize,
                                SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0),// texture.Description.SampleDescription,
                                BindFlags = BindFlags.None,
                                MipLevels = 1,//texture.Description.MipLevels,
                                OptionFlags = texture.Description.OptionFlags
                            });

                            // Copy the subresource region, we are dealing with a flat 2D texture with no MipMapping, so 0 is the subresource index
                            theTexture.Device.ImmediateContext.CopySubresourceRegion(theTexture, 0, new ResourceRegion()
                            {
                                Top = regionToCapture.Top,
                                Bottom = regionToCapture.Bottom,
                                Left = regionToCapture.Left,
                                Right = regionToCapture.Right,
                                Front = 0,
                                Back = 1 // Must be 1 or only black will be copied
                            }, textureDest, 0, 0, 0, 0);

                            // Note: it would be possible to capture multiple frames and process them in a background thread

                            // Copy to memory and send back to host process on a background thread so that we do not cause any delay in the rendering pipeline
                            Guid requestId = this.Request.RequestId; // this.Request gets set to null, so copy the RequestId for use in the thread
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                //FileStream fs = new FileStream(@"c:\temp\temp.bmp", FileMode.Create);
                                //Texture2D.ToStream(testSubResourceCopy, ImageFileFormat.Bmp, fs);

                                DateTime startCopyToSystemMemory = DateTime.Now;
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    Texture2D.ToStream(textureDest.Device.ImmediateContext, textureDest, ImageFileFormat.Bmp, ms);
                                    ms.Position = 0;
                                    this.DebugMessage("PresentHook: Copy to System Memory time: " + (DateTime.Now - startCopyToSystemMemory).ToString());

                                    DateTime startSendResponse = DateTime.Now;
                                    SendResponse(ms, requestId);
                                    this.DebugMessage("PresentHook: Send response time: " + (DateTime.Now - startSendResponse).ToString());
                                }

                                // Free the textureDest as we no longer need it.
                                textureDest.Dispose();
                                textureDest = null;
                                this.DebugMessage("PresentHook: Full Capture time: " + (DateTime.Now - startTime).ToString());
                            });

                            // Prevent the request from being processed a second time
                            this.Request = null;

                            // Make sure we free up the resolved texture if it was created
                            if (textureResolved != null)
                            {
                                textureResolved.Dispose();
                                textureResolved = null;
                            }
                        }
                        this.DebugMessage("PresentHook: Copy BackBuffer time: " + (DateTime.Now - startTime).ToString());
                        this.DebugMessage("PresentHook: Request End");
                    }
                    #endregion

                    #region TODO: Draw overlay (after screenshot so we don't capture overlay as well)
                    if (this.ShowOverlay)
                    {
                        // Note: Direct3D 11 doesn't have font support, so I believe the approach is to use
                        //       a Direct3D 10.1 device with Direct2d, render to a texture, and then blend
                        //       this into the Direct3D 11 backbuffer - hmm sounds like fun.
                        // http://forums.create.msdn.com/forums/t/38961.aspx
                        // http://www.gamedev.net/topic/547920-how-to-use-d2d-with-d3d11/
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    // If there is an error we do not want to crash the hooked application, so swallow the exception
                    this.DebugMessage("PresentHook: Exeception: " + e.GetType().FullName + ": " + e.Message);
                    //return unchecked((int)0x8000FFFF); //E_UNEXPECTED
                }

                // As always we need to call the original method, note that EasyHook has already repatched the original method
                // so calling it here will not cause an endless recursion to this function
                return swapChain.Present(syncInterval, flags).Code;
            }
        }
    }
}
