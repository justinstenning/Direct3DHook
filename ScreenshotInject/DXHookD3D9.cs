using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D9;
using EasyHook;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing;

namespace ScreenshotInject
{
    internal class DXHookD3D9: BaseDXHook
    {
        public DXHookD3D9(ScreenshotInterface.ScreenshotInterface ssInterface)
            : base(ssInterface)
        {
        }

        LocalHook Direct3DDevice_EndSceneHook = null;
        LocalHook Direct3DDevice_ResetHook = null;
        object _lockRenderTarget = new object();
        Surface _renderTarget;

        protected override string HookName
        {
            get
            {
                return "DXHookD3D9";
            }
        }

        const int D3D9_DEVICE_METHOD_COUNT = 119;
        public override void Hook()
        {
            this.DebugMessage("Hook: Begin");
            // First we need to determine the function address for IDirect3DDevice9
            Device device;
            List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
            this.DebugMessage("Hook: Before device creation");
            using (Direct3D d3d = new Direct3D())
            {
                this.DebugMessage("Hook: Device created");
                using (device = new Device(d3d, 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
                {
                    id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.ComPointer, D3D9_DEVICE_METHOD_COUNT));
                }
            }

            // We want to hook each method of the IDirect3DDevice9 interface that we are interested in

            // 42 - EndScene (we will retrieve the back buffer here)
            Direct3DDevice_EndSceneHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene],
                // On Windows 7 64-bit w/ 32-bit app and d3d9 dll version 6.1.7600.16385, the address is equiv to:
                // (IntPtr)(GetModuleHandle("d3d9").ToInt32() + 0x1ce09),
                // A 64-bit app would use 0xff18
                // Note: GetD3D9DeviceFunctionAddress will output these addresses to a log file
                new Direct3D9Device_EndSceneDelegate(EndSceneHook),
                this);

            // 16 - Reset (called on resolution change or windowed/fullscreen change - we will reset some things as well)
            Direct3DDevice_ResetHook = LocalHook.Create(
                id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset],
                // On Windows 7 64-bit w/ 32-bit app and d3d9 dll version 6.1.7600.16385, the address is equiv to:
                //(IntPtr)(GetModuleHandle("d3d9").ToInt32() + 0x58dda),
                // A 64-bit app would use 0x3b3a0
                // Note: GetD3D9DeviceFunctionAddress will output these addresses to a log file
                new Direct3D9Device_ResetDelegate(ResetHook),
                this);

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */
            Direct3DDevice_EndSceneHook.ThreadACL.SetExclusiveACL(new Int32[1]);

            Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new Int32[1]);

            this.DebugMessage("Hook: End");
        }

        /// <summary>
        /// Just ensures that the surface we created is cleaned up.
        /// </summary>
        public override void Cleanup()
        {
            try
            {
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }

                    Request = null;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The IDirect3DDevice9.EndScene function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int Direct3D9Device_EndSceneDelegate(IntPtr device);

        /// <summary>
        /// The IDirect3DDevice9.Reset function definition
        /// </summary>
        /// <param name="device"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate int Direct3D9Device_ResetDelegate(IntPtr device, ref D3DPRESENT_PARAMETERS presentParameters);

        /// <summary>
        /// Reset the _renderTarget so that we are sure it will have the correct presentation parameters (required to support working across changes to windowed/fullscreen or resolution changes)
        /// </summary>
        /// <param name="devicePtr"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        int ResetHook(IntPtr devicePtr, ref D3DPRESENT_PARAMETERS presentParameters)
        {
            using (Device device = Device.FromPointer(devicePtr))
            {
                PresentParameters pp = new PresentParameters()
                {
                    AutoDepthStencilFormat = (Format)presentParameters.AutoDepthStencilFormat,
                    BackBufferCount = presentParameters.BackBufferCount,
                    BackBufferFormat = (Format)presentParameters.BackBufferFormat,
                    BackBufferHeight = presentParameters.BackBufferHeight,
                    BackBufferWidth = presentParameters.BackBufferWidth,
                    DeviceWindowHandle = presentParameters.DeviceWindowHandle,
                    EnableAutoDepthStencil = presentParameters.EnableAutoDepthStencil,
                    FullScreenRefreshRateInHertz = presentParameters.FullScreen_RefreshRateInHz,
                    Multisample = (MultisampleType)presentParameters.MultiSampleType,
                    MultisampleQuality = presentParameters.MultiSampleQuality,
                    PresentationInterval = (PresentInterval)presentParameters.PresentationInterval,
                    PresentFlags = (PresentFlags)presentParameters.Flags,
                    SwapEffect = (SwapEffect)presentParameters.SwapEffect,
                    Windowed = presentParameters.Windowed
                };
                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }
                }
                // EasyHook has already repatched the original Reset so calling it here will not cause an endless recursion to this function
                return device.Reset(pp).Code;
            }
        }

        TimeSpan _lastScreenshotTime = new TimeSpan(0);

        DateTime? _lastFrame;
        
        // Used in the overlay
        DateTime? _lastRequestTime;
        SlimDX.Vector2[] _lineVectors = null;
        float _lineAlpha = 1;

        /// <summary>
        /// Hook for IDirect3DDevice9.EndScene
        /// </summary>
        /// <param name="devicePtr">Pointer to the IDirect3DDevice9 instance. Note: object member functions always pass "this" as the first parameter.</param>
        /// <returns>The HRESULT of the original EndScene</returns>
        /// <remarks>Remember that this is called many times a second by the Direct3D application - be mindful of memory and performance!</remarks>
        int EndSceneHook(IntPtr devicePtr)
        {
            using (Device device = Device.FromPointer(devicePtr))
            {
                // If you need to capture at a particular frame rate, add logic here decide whether or not to skip the frame
                try
                {
                    #region Screenshot Request
                    // Is there a screenshot request? If so lets grab the backbuffer
                    lock (_lockRenderTarget)
                    {
                        if (Request != null)
                        {
                            _lastRequestTime = DateTime.Now;
                            DateTime start = DateTime.Now;
                            try
                            {
                                // First ensure we have a Surface to the render target data into
                                if (_renderTarget == null)
                                {
                                    // Create offscreen surface to use as copy of render target data
                                    using (SwapChain sc = device.GetSwapChain(0))
                                    {
                                        _renderTarget = Surface.CreateOffscreenPlain(device, sc.PresentParameters.BackBufferWidth, sc.PresentParameters.BackBufferHeight, sc.PresentParameters.BackBufferFormat, Pool.SystemMemory);
                                    }
                                }

                                #region Prepare lines for overlay
                                if (this.Request.RegionToCapture.Width == 0)
                                {
                                    _lineVectors = new SlimDX.Vector2[] { 
                                        new SlimDX.Vector2(0, 0),
                                        new SlimDX.Vector2(_renderTarget.Description.Width - 1, _renderTarget.Description.Height - 1),
                                        new SlimDX.Vector2(0, _renderTarget.Description.Height - 1),
                                        new SlimDX.Vector2(_renderTarget.Description.Width - 1, 0),
                                        new SlimDX.Vector2(0, 0),
                                        new SlimDX.Vector2(0, _renderTarget.Description.Height - 1),
                                        new SlimDX.Vector2(_renderTarget.Description.Width - 1, _renderTarget.Description.Height - 1),
                                        new SlimDX.Vector2(_renderTarget.Description.Width - 1, 0),
                                    };
                                }
                                else
                                {
                                    _lineVectors = new SlimDX.Vector2[] { 
                                        new SlimDX.Vector2(this.Request.RegionToCapture.X, this.Request.RegionToCapture.Y),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.Right, this.Request.RegionToCapture.Bottom),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.X, this.Request.RegionToCapture.Bottom),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.Right, this.Request.RegionToCapture.Y),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.X, this.Request.RegionToCapture.Y),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.X, this.Request.RegionToCapture.Bottom),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.Right, this.Request.RegionToCapture.Bottom),
                                        new SlimDX.Vector2(this.Request.RegionToCapture.Right, this.Request.RegionToCapture.Y),
                                    };
                                }
                                #endregion

                                using (Surface backBuffer = device.GetBackBuffer(0, 0))
                                {
                                    // Create a super fast copy of the back buffer on our Surface
                                    device.GetRenderTargetData(backBuffer, _renderTarget);

                                    // We have the back buffer data and can now work on copying it to a bitmap
                                    ProcessRequest();
                                }
                            }
                            finally
                            {
                                // We have completed the request - mark it as null so we do not continue to try to capture the same request
                                // Note: If you are after high frame rates, consider implementing buffers here to capture more frequently
                                //         and send back to the host application as needed. The IPC overhead significantly slows down 
                                //         the whole process if sending frame by frame.
                                Request = null;
                            }
                            DateTime end = DateTime.Now;
                            this.DebugMessage("EndSceneHook: Capture time: " + (end - start).ToString());
                            _lastScreenshotTime = (end - start);
                        }
                    }
                    #endregion

                    #region Example: Draw Overlay (after screenshot so we don't capture overlay as well)

                    if (this.ShowOverlay)
                    {
                        #region Draw fading lines based on last screencapture request
                        if (_lastRequestTime != null && _lineVectors != null)
                        {
                            TimeSpan timeSinceRequest = DateTime.Now - _lastRequestTime.Value;
                            if (timeSinceRequest.TotalMilliseconds < 1000.0)
                            {
                                using (Line line = new Line(device))
                                {
                                    _lineAlpha = (float)((1000.0 - timeSinceRequest.TotalMilliseconds) / 1000.0); // This is our fade out
                                    line.Antialias = true;
                                    line.Width = 1.0f;
                                    line.Begin();
                                    line.Draw(_lineVectors, new SlimDX.Color4(_lineAlpha, 0.5f, 0.5f, 1.0f));
                                    line.End();
                                }
                            }
                            else
                            {
                                _lineVectors = null;
                            }
                        }
                        #endregion

                        #region Draw frame rate
                        using (SlimDX.Direct3D9.Font font = new SlimDX.Direct3D9.Font(device, new System.Drawing.Font("Times New Roman", 16.0f)))
                        {
                            if (_lastFrame != null)
                            {
                                font.DrawString(null, String.Format("{0:N1} fps", (1000.0 / (DateTime.Now - _lastFrame.Value).TotalMilliseconds)), 100, 100, System.Drawing.Color.Red);
                            }
                            _lastFrame = DateTime.Now;
                        }
                        #endregion
                    }

                    #endregion
                }
                catch(Exception e)
                {
                    // If there is an error we do not want to crash the hooked application, so swallow the exception
                    this.DebugMessage("EndSceneHook: Exeception: " + e.GetType().FullName + ": " + e.Message);
                }

                // EasyHook has already repatched the original EndScene - so calling EndScene against the SlimDX device will call the original
                // EndScene and bypass this hook. EasyHook will automatically reinstall the hook after this hook function exits.
                return device.EndScene().Code;
            }
        }

        /// <summary>
        /// Copies the _renderTarget surface into a stream and starts a new thread to send the data back to the host process
        /// </summary>
        void ProcessRequest()
        {
            if (Request != null)
            {
                // SlimDX now uses a marshal_as for Rectangle to RECT that correctly does the conversion for us, therefore no need
                // to adjust the region Width/Height to fit the x1,y1,x2,y2 format.
                Rectangle region = Request.RegionToCapture;
                
                // Prepare the parameters for RetrieveImageData to be called in a separate thread.
                RetrieveImageDataParams retrieveParams = new RetrieveImageDataParams();

                // After the Stream is created we are now finished with _renderTarget and have our own separate copy of the data,
                // therefore it will now be safe to begin a new thread to complete processing.
                // Note: RetrieveImageData will take care of closing the stream.
                // Note 2: Surface.ToStream is the slowest part of the screen capture process - the other methods
                //         available to us at this point are _renderTarget.GetDC(), and _renderTarget.LockRectangle/UnlockRectangle
                if (Request.RegionToCapture.Width == 0)
                {
                    // The width is 0 so lets grab the entire window
                    retrieveParams.Stream = Surface.ToStream(_renderTarget, ImageFileFormat.Bmp);
                }
                else if (Request.RegionToCapture.Height > 0)
                {
                    retrieveParams.Stream = Surface.ToStream(_renderTarget, ImageFileFormat.Bmp, region);
                }

                if (retrieveParams.Stream != null)
                {
                    // _screenshotRequest will most probably be null by the time RetrieveImageData is executed 
                    // in a new thread, therefore we must provide the RequestId separately.
                    retrieveParams.RequestId = Request.RequestId;

                    // Begin a new thread to process the image data and send the request result back to the host application
                    Thread t = new Thread(new ParameterizedThreadStart(RetrieveImageData));
                    t.Start(retrieveParams);
                }
            }
        }

        /// <summary>
        /// Used to hold the parameters to be passed to RetrieveImageData
        /// </summary>
        struct RetrieveImageDataParams
        {
            internal Stream Stream;
            internal Guid RequestId;
        }

        /// <summary>
        /// ParameterizedThreadStart method that places the image data from the stream into a byte array and then sets the Interface screenshot response. This can be called asynchronously.
        /// </summary>
        /// <param name="param">An instance of RetrieveImageDataParams is required to be passed as the parameter.</param>
        /// <remarks>The stream object passed will be closed!</remarks>
        void RetrieveImageData(object param)
        {
            RetrieveImageDataParams retrieveParams = (RetrieveImageDataParams)param;
            try
            {
                SendResponse(retrieveParams.Stream, retrieveParams.RequestId);
            }
            finally
            {
                retrieveParams.Stream.Close();
            }
        }
    }
}
