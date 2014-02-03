using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SlimDX.Direct3D9;
using EasyHook;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing;
using Capture.Interface;
using SharpDX.Direct3D9;

namespace Capture.Hook
{
    internal class DXHookD3D9: BaseDXHook
    {
        public DXHookD3D9(CaptureInterface ssInterface)
            : base(ssInterface)
        {
        }

        LocalHook Direct3DDevice_EndSceneHook = null;
        LocalHook Direct3DDevice_ResetHook = null;
        LocalHook Direct3DDevice_PresentHook = null;
        LocalHook Direct3DDeviceEx_PresentExHook = null;
        object _lockRenderTarget = new object();
        Surface _renderTarget;

        protected override string HookName
        {
            get
            {
                return "DXHookD3D9";
            }
        }
        List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();
        //List<IntPtr> id3dDeviceExFunctionAddresses = new List<IntPtr>();
        const int D3D9_DEVICE_METHOD_COUNT = 119;
        const int D3D9Ex_DEVICE_METHOD_COUNT = 15;
        bool _supportsDirect3D9Ex = false;
        public override void Hook()
        {
            this.DebugMessage("Hook: Begin");
            // First we need to determine the function address for IDirect3DDevice9
            Device device;
            id3dDeviceFunctionAddresses = new List<IntPtr>();
            //id3dDeviceExFunctionAddresses = new List<IntPtr>();
            this.DebugMessage("Hook: Before device creation");
            using (Direct3D d3d = new Direct3D())
            {
                using (var renderForm = new System.Windows.Forms.Form())
                {
                    using (device = new Device(d3d, 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1, DeviceWindowHandle = renderForm.Handle }))
                    {
                        this.DebugMessage("Hook: Device created");
                        id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(device.NativePointer, D3D9_DEVICE_METHOD_COUNT));
                    }
                }
            }

            try
            {
                using (Direct3DEx d3dEx = new Direct3DEx())
                {
                    this.DebugMessage("Hook: Direct3DEx...");
                    using (var renderForm = new System.Windows.Forms.Form())
                    {
                        using (var deviceEx = new DeviceEx(d3dEx, 0, DeviceType.NullReference, IntPtr.Zero, CreateFlags.HardwareVertexProcessing, new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1, DeviceWindowHandle = renderForm.Handle }, new DisplayModeEx() { Width = 800, Height = 600 }))
                        {
                            this.DebugMessage("Hook: DeviceEx created - PresentEx supported");
                            id3dDeviceFunctionAddresses.AddRange(GetVTblAddresses(deviceEx.NativePointer, D3D9_DEVICE_METHOD_COUNT, D3D9Ex_DEVICE_METHOD_COUNT));
                            _supportsDirect3D9Ex = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                _supportsDirect3D9Ex = false;
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

            unsafe
            {
                // If Direct3D9Ex is available - hook the PresentEx
                if (_supportsDirect3D9Ex)
                {
                    Direct3DDeviceEx_PresentExHook = LocalHook.Create(
                        id3dDeviceFunctionAddresses[(int)Direct3DDevice9ExFunctionOrdinals.PresentEx],
                        new Direct3D9DeviceEx_PresentExDelegate(PresentExHook),
                        this);
                }

                // Always hook Present also (device will only call Present or PresentEx not both)
                Direct3DDevice_PresentHook = LocalHook.Create(
                    id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present],
                    new Direct3D9Device_PresentDelegate(PresentHook),
                    this);
            }

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
            Hooks.Add(Direct3DDevice_EndSceneHook);

            Direct3DDevice_PresentHook.ThreadACL.SetExclusiveACL(new Int32[1]);
            Hooks.Add(Direct3DDevice_PresentHook);

            if (_supportsDirect3D9Ex)
            {
                Direct3DDeviceEx_PresentExHook.ThreadACL.SetExclusiveACL(new Int32[1]);
                Hooks.Add(Direct3DDeviceEx_PresentExHook);
            }

            Direct3DDevice_ResetHook.ThreadACL.SetExclusiveACL(new Int32[1]);
            Hooks.Add(Direct3DDevice_ResetHook);

            this.DebugMessage("Hook: End");
        }

        /// <summary>
        /// Just ensures that the surface we created is cleaned up.
        /// </summary>
        public override void Cleanup()
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (true)
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
            base.Dispose(disposing);
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
        delegate int Direct3D9Device_ResetDelegate(IntPtr device, ref PresentParameters presentParameters);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        unsafe delegate int Direct3D9Device_PresentDelegate(IntPtr devicePtr, SharpDX.Rectangle* pSourceRect, SharpDX.Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        unsafe delegate int Direct3D9DeviceEx_PresentExDelegate(IntPtr devicePtr, SharpDX.Rectangle* pSourceRect, SharpDX.Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion, Present dwFlags);
        

        /// <summary>
        /// Reset the _renderTarget so that we are sure it will have the correct presentation parameters (required to support working across changes to windowed/fullscreen or resolution changes)
        /// </summary>
        /// <param name="devicePtr"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        int ResetHook(IntPtr devicePtr, ref PresentParameters presentParameters)
        {
            Device device = (Device)devicePtr;
            try
            {

                lock (_lockRenderTarget)
                {
                    if (_renderTarget != null)
                    {
                        _renderTarget.Dispose();
                        _renderTarget = null;
                    }
                }
                // EasyHook has already repatched the original Reset so calling it here will not cause an endless recursion to this function
                device.Reset(presentParameters);
                return SharpDX.Result.Ok.Code;
            }
            catch (SharpDX.SharpDXException sde)
            {
                return sde.ResultCode.Code;
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
                return SharpDX.Result.Ok.Code;
            }
        }

        bool _isUsingPresent = false;

        // Used in the overlay
        unsafe int PresentExHook(IntPtr devicePtr, SharpDX.Rectangle* pSourceRect, SharpDX.Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion, Present dwFlags)
        {
            _isUsingPresent = true;
            DeviceEx device = (DeviceEx)devicePtr;

            DoCaptureRenderTarget(device, "PresentEx");

            //    Region region = new Region(pDirtyRegion);
            if (pSourceRect == null || *pSourceRect == SharpDX.Rectangle.Empty)
                device.PresentEx(dwFlags);
            else
            {
                if (hDestWindowOverride != IntPtr.Zero)
                    device.PresentEx(dwFlags, *pSourceRect, *pDestRect, hDestWindowOverride);
                else
                    device.PresentEx(dwFlags, *pSourceRect, *pDestRect);
            }
            return SharpDX.Result.Ok.Code;
        }
        
        unsafe int PresentHook(IntPtr devicePtr, SharpDX.Rectangle* pSourceRect, SharpDX.Rectangle* pDestRect, IntPtr hDestWindowOverride, IntPtr pDirtyRegion)
        {
            // Example of using delegate to original function pointer to call original method
            //var original = (Direct3D9Device_PresentDelegate)(Object)Marshal.GetDelegateForFunctionPointer(id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present], typeof(Direct3D9Device_PresentDelegate));
            //try
            //{
            //    unsafe
            //    {
            //        return original(devicePtr, ref pSourceRect, ref pDestRect, hDestWindowOverride, pDirtyRegion);
            //    }
            //}
            //catch { }
            _isUsingPresent = true;

            Device device = (Device)devicePtr;

            DoCaptureRenderTarget(device, "PresentHook");

            if (pSourceRect == null || *pSourceRect == SharpDX.Rectangle.Empty)
                device.Present();
            else
            {
                if (hDestWindowOverride != IntPtr.Zero)
                    device.Present(*pSourceRect, *pDestRect, hDestWindowOverride);
                else
                    device.Present(*pSourceRect, *pDestRect);
            }
            return SharpDX.Result.Ok.Code;
        }

        /// <summary>
        /// Hook for IDirect3DDevice9.EndScene
        /// </summary>
        /// <param name="devicePtr">Pointer to the IDirect3DDevice9 instance. Note: object member functions always pass "this" as the first parameter.</param>
        /// <returns>The HRESULT of the original EndScene</returns>
        /// <remarks>Remember that this is called many times a second by the Direct3D application - be mindful of memory and performance!</remarks>
        int EndSceneHook(IntPtr devicePtr)
        {
            Device device = (Device)devicePtr;

            if (!_isUsingPresent)
                DoCaptureRenderTarget(device, "EndSceneHook");

            device.EndScene();
            return SharpDX.Result.Ok.Code;
        }
        /// <summary>
        /// Implementation of capturing from the render target of the Direct3D9 Device (or DeviceEx)
        /// </summary>
        /// <param name="device"></param>
        void DoCaptureRenderTarget(Device device, string hook)
        {
            this.Frame();

            try
            {
                    #region Screenshot Request
                // Single frame capture request
                if (this.Request != null)
                {
                    DateTime start = DateTime.Now;
                    try
                    {

                        using (Surface renderTargetTemp = device.GetRenderTarget(0))
                        {
                            int width, height;

                            // TODO: If resizing the captured image is required it can be adjusted here
                            //if (renderTargetTemp.Description.Width > 1280)
                            //{
                            //    width = 1280;
                            //    height = (int)Math.Round((renderTargetTemp.Description.Height * (1280.0 / renderTargetTemp.Description.Width)));
                            //}
                            //else
                            {
                                width = renderTargetTemp.Description.Width;
                                height = renderTargetTemp.Description.Height;
                            }

                            // First ensure we have a Surface to the render target data into
                            if (_renderTarget == null)
                            {
                                // Create offscreen surface to use as copy of render target data
                                using (SwapChain sc = device.GetSwapChain(0))
                                {
                                    _renderTarget = Surface.CreateOffscreenPlain(device, width, height, sc.PresentParameters.BackBufferFormat, Pool.SystemMemory);
                                }
                            }

                            // Create our resolved surface (resizing if necessary and to resolve any multi-sampling)
                            using (Surface resolvedSurface = Surface.CreateRenderTarget(device, width, height, renderTargetTemp.Description.Format, MultisampleType.None, 0, false))
                            {
                                // Resize from Render Surface to resolvedSurface
                                device.StretchRectangle(renderTargetTemp, resolvedSurface, TextureFilter.None);

                                // Get Render Data
                                device.GetRenderTargetData(resolvedSurface, _renderTarget);
                            }
                        }

                        if (Request != null)
                            ProcessRequest();
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
                    this.DebugMessage(hook + ": Capture time: " + (end - start).ToString());
                }

                #endregion

                if (this.Config.ShowOverlay)
                {
                    #region Draw frame rate

                    // TODO: font needs to be created and then reused, not created each frame!
                    using (SharpDX.Direct3D9.Font font = new SharpDX.Direct3D9.Font(device, new FontDescription()
                                    {
                                        Height = 16,
                                        FaceName = "Arial",
                                        Italic = false,
                                        Width = 0,
                                        MipLevels = 1,
                                        CharacterSet = FontCharacterSet.Default,
                                        OutputPrecision = FontPrecision.Default,
                                        Quality = FontQuality.Antialiased,
                                        PitchAndFamily = FontPitchAndFamily.Default | FontPitchAndFamily.DontCare,
                                        Weight = FontWeight.Bold
                                    }))
                    {

                        if (this.FPS.GetFPS() >= 1)
                        {
                            font.DrawText(null, String.Format("{0:N0} fps", this.FPS.GetFPS()), 5, 5, SharpDX.Color.Red);
                        }

                        if (this.TextDisplay != null && this.TextDisplay.Display)
                        {
                            font.DrawText(null, this.TextDisplay.Text, 5, 25, new SharpDX.ColorBGRA(255, 0, 0, (byte)Math.Round((Math.Abs(1.0f - TextDisplay.Remaining) * 255f))));
                        }
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }
        
        /// <summary>
        /// Copies the _renderTarget surface into a stream and starts a new thread to send the data back to the host process
        /// </summary>
        void ProcessRequest()
        {
            if (Request != null)
            {
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
                    // The width is 0 so lets grab the entire surface
                    retrieveParams.Stream = Surface.ToStream(_renderTarget, ImageFileFormat.Bmp);
                }
                else if (Request.RegionToCapture.Height > 0)
                {
                    retrieveParams.Stream = Surface.ToStream(_renderTarget, ImageFileFormat.Bmp, new SharpDX.Rectangle(region.Left, region.Top, region.Right, region.Bottom));
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
                ProcessCapture(retrieveParams.Stream, retrieveParams.RequestId);
            }
            finally
            {
                retrieveParams.Stream.Close();
            }
        }
    }
}
