using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Capture.Interface;

using SharpDX.Direct3D9;

namespace Capture.Hook
{
    using SharpDX;

    internal class DXHookD3D9 : BaseDXHook
    {
        #region Constants

        private const int D3D9Ex_DEVICE_METHOD_COUNT = 15;

        private const int D3D9_DEVICE_METHOD_COUNT = 119;

        #endregion

        #region Fields

        private readonly object endSceneLock = new object();

        private readonly object renderTargetLock = new object();

        private readonly object surfaceLock = new object();

        private HookData<Direct3D9DeviceEx_PresentExDelegate> Direct3DDeviceEx_PresentExHook = null;

        private HookData<Direct3D9DeviceEx_ResetExDelegate> Direct3DDeviceEx_ResetExHook = null;

        private HookData<Direct3D9Device_EndSceneDelegate> Direct3DDevice_EndSceneHook = null;

        private HookData<Direct3D9Device_PresentDelegate> Direct3DDevice_PresentHook = null;

        private HookData<Direct3D9Device_ResetDelegate> Direct3DDevice_ResetHook = null;

        private IntPtr currentDevice;

        private int endSceneHookRecurse;

        private Format format;

        private int height;

        private bool hooksStarted;

        private List<IntPtr> id3dDeviceFunctionAddresses = new List<IntPtr>();

        private bool isUsingPresent;

        private int pitch;

        private int presentHookRecurse = 0;

        private Query query;

        private bool queryIssued;

        private Surface renderTarget;

        private bool supportsDirect3DEx = false;

        private Surface surface;

        private IntPtr surfaceDataPointer;

        private bool surfaceLocked;

        private bool surfacesSetup;

        private int width;

        #endregion

        #region Constructors and Destructors

        public DXHookD3D9(CaptureInterface ssInterface)
            : base(ssInterface)
        {
        }

        #endregion

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9DeviceEx_PresentExDelegate(
            IntPtr devicePtr, 
            Rectangle* pSourceRect, 
            Rectangle* pDestRect, 
            IntPtr hDestWindowOverride, 
            IntPtr pDirtyRegion, 
            Present dwFlags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9DeviceEx_ResetExDelegate(IntPtr devicePtr, ref PresentParameters presentParameters, DisplayModeEx displayModeEx);

        /// <summary>
        /// The IDirect3DDevice9.EndScene function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_EndSceneDelegate(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate int Direct3D9Device_PresentDelegate(
            IntPtr devicePtr, 
            Rectangle* pSourceRect, 
            Rectangle* pDestRect, 
            IntPtr hDestWindowOverride, 
            IntPtr pDirtyRegion);

        /// <summary>
        /// The IDirect3DDevice9.Reset function definition
        /// </summary>
        /// <param name="device"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int Direct3D9Device_ResetDelegate(IntPtr device, ref PresentParameters presentParameters);

        #endregion

        #region Properties

        protected override string HookName
        {
            get
            {
                return "DXHookD3D9";
            }
        }

        #endregion

        #region Public Methods and Operators

        public override void Cleanup()
        {
            // ClearData();
        }

        public override unsafe void Hook()
        {
            this.DebugMessage("Hook: Begin");

            this.DebugMessage("Hook: Before device creation");
            using (var d3d = new Direct3D())
            {
                this.DebugMessage("Hook: Direct3D created");
                using (
                    var device = new Device(
                        d3d, 
                        0, 
                        DeviceType.NullReference, 
                        IntPtr.Zero, 
                        CreateFlags.HardwareVertexProcessing, 
                        new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }))
                {
                    this.id3dDeviceFunctionAddresses.AddRange(this.GetVTblAddresses(device.NativePointer, D3D9_DEVICE_METHOD_COUNT));
                }
            }

            try
            {
                using (var d3dEx = new Direct3DEx())
                {
                    this.DebugMessage("Hook: Try Direct3DEx...");
                    using (
                        var deviceEx = new DeviceEx(
                            d3dEx, 
                            0, 
                            DeviceType.NullReference, 
                            IntPtr.Zero, 
                            CreateFlags.HardwareVertexProcessing, 
                            new PresentParameters() { BackBufferWidth = 1, BackBufferHeight = 1 }, 
                            new DisplayModeEx() { Width = 800, Height = 600 }))
                    {
                        this.id3dDeviceFunctionAddresses.AddRange(
                            this.GetVTblAddresses(deviceEx.NativePointer, D3D9_DEVICE_METHOD_COUNT, D3D9Ex_DEVICE_METHOD_COUNT));
                        this.supportsDirect3DEx = true;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            DebugMessage("Setting up Direct3D hooks...");
            this.Direct3DDevice_EndSceneHook =
                new HookData<Direct3D9Device_EndSceneDelegate>(
                    this.id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.EndScene], 
                    new Direct3D9Device_EndSceneDelegate(this.EndSceneHook), 
                    this);

            this.Direct3DDevice_EndSceneHook.ReHook();
            this.Hooks.Add(this.Direct3DDevice_EndSceneHook.Hook);

            this.Direct3DDevice_PresentHook =
                new HookData<Direct3D9Device_PresentDelegate>(
                    this.id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Present], 
                    new Direct3D9Device_PresentDelegate(this.PresentHook), 
                    this);

            this.Direct3DDevice_ResetHook =
                new HookData<Direct3D9Device_ResetDelegate>(
                    this.id3dDeviceFunctionAddresses[(int)Direct3DDevice9FunctionOrdinals.Reset], 
                    new Direct3D9Device_ResetDelegate(this.ResetHook), 
                    this);

            if (this.supportsDirect3DEx)
            {
                DebugMessage("Setting up Direct3DEx hooks...");
                this.Direct3DDeviceEx_PresentExHook =
                    new HookData<Direct3D9DeviceEx_PresentExDelegate>(
                        this.id3dDeviceFunctionAddresses[(int)Direct3DDevice9ExFunctionOrdinals.PresentEx], 
                        new Direct3D9DeviceEx_PresentExDelegate(this.PresentExHook), 
                        this);

                this.Direct3DDeviceEx_ResetExHook =
                    new HookData<Direct3D9DeviceEx_ResetExDelegate>(
                        this.id3dDeviceFunctionAddresses[(int)Direct3DDevice9ExFunctionOrdinals.ResetEx], 
                        new Direct3D9DeviceEx_ResetExDelegate(this.ResetExHook), 
                        this);
            }

            this.Direct3DDevice_ResetHook.ReHook();
            this.Hooks.Add(this.Direct3DDevice_ResetHook.Hook);

            this.Direct3DDevice_PresentHook.ReHook();
            this.Hooks.Add(this.Direct3DDevice_PresentHook.Hook);

            if (this.supportsDirect3DEx)
            {
                this.Direct3DDeviceEx_PresentExHook.ReHook();
                this.Hooks.Add(this.Direct3DDeviceEx_PresentExHook.Hook);

                this.Direct3DDeviceEx_ResetExHook.ReHook();
                this.Hooks.Add(this.Direct3DDeviceEx_ResetExHook.Hook);
            }

            this.DebugMessage("Hook: End");
        }

        #endregion

        #region Methods

        protected override void Dispose(bool disposing)
        {
            if (true)
            {
                try
                {
                    ClearData();
                }
                catch
                {
                }
            }
            base.Dispose(disposing);
        }

        private void ClearData()
        {
            DebugMessage("ClearData called");

            // currentDevice = null;
            this.Request = null;
            width = 0;
            height = 0;
            pitch = 0;
            surfacesSetup = false;
            this.hooksStarted = false;
            if (surfaceLocked)
            {
                lock (surfaceLock)
                {
                    surface.UnlockRectangle();
                    surfaceLocked = false;
                }
            }

            if (surface != null)
            {
                surface.Dispose();
                surface = null;
            }
            if (renderTarget != null)
            {
                renderTarget.Dispose();
                renderTarget = null;
            }
            if (query != null)
            {
                query.Dispose();
                query = null;
                queryIssued = false;
            }
        }

        /// <summary>
        /// Implementation of capturing from the render target of the Direct3D9 Device (or DeviceEx)
        /// </summary>
        /// <param name="device"></param>
        private void DoCaptureRenderTarget(Device device, string hook)
        {
            // this.Frame();

            try
            {
                if (!surfacesSetup)
                {
                    using (Surface backbuffer = device.GetRenderTarget(0))
                    {
                        format = backbuffer.Description.Format;
                        width = backbuffer.Description.Width;
                        height = backbuffer.Description.Height;
                    }

                    SetupSurfaces(device);
                }

                if (!surfacesSetup)
                {
                    return;
                }

                if (Request != null)
                {
                    HandleCaptureRequest(device);
                }
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }

        /// <summary>
        /// Hook for IDirect3DDevice9.EndScene
        /// </summary>
        /// <param name="devicePtr">Pointer to the IDirect3DDevice9 instance. Note: object member functions always pass "this" as the first parameter.</param>
        /// <returns>The HRESULT of the original EndScene</returns>
        /// <remarks>Remember that this is called many times a second by the Direct3D application - be mindful of memory and performance!</remarks>
        private int EndSceneHook(IntPtr devicePtr)
        {
            int hresult = Result.Ok.Code;
            var device = (Device)devicePtr;
            try
            {
                if (endSceneHookRecurse == 0)
                {
                    if (!hooksStarted)
                    {
                        DebugMessage("EndSceneHook: hooks not started");
                        SetupData(device);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            endSceneHookRecurse++;
            hresult = Direct3DDevice_EndSceneHook.Original(devicePtr);
            endSceneHookRecurse--;
            return hresult;
        }

        private void HandleCaptureRequest(Device device)
        {
            if (Request == null)
            {
                return;
            }
            try
            {
                bool tmp;
                if (queryIssued && query.GetData(out tmp, false))
                {
                    queryIssued = false;
                    var lockedRect = surface.LockRectangle(LockFlags.ReadOnly);
                    surfaceDataPointer = lockedRect.DataPointer;
                    surfaceLocked = true;

                    new Thread(HandleCaptureRequestThread).Start();
                }

                using (var backbuffer = device.GetBackBuffer(0, 0))
                {
                    device.StretchRectangle(backbuffer, renderTarget, TextureFilter.None);
                }

                if (surfaceLocked)
                {
                    lock (renderTargetLock)
                    {
                        if (!this.surfaceLocked)
                        {
                            return;
                        }
                        this.surface.UnlockRectangle();
                        this.surfaceLocked = false;
                    }
                }

                try
                {
                    device.GetRenderTargetData(renderTarget, surface);
                    query.Issue(Issue.End);
                    queryIssued = true;
                }
                catch (Exception ex)
                {
                    DebugMessage(ex.ToString());
                }
            }
            catch (Exception e)
            {
                DebugMessage(e.ToString());
            }
        }

        private void HandleCaptureRequestThread()
        {
            try
            {
                if (Request == null)
                {
                    return;
                }

                lock (renderTargetLock)
                {
                    var size = height * pitch;
                    var bdata = new byte[size];
                    Marshal.Copy(surfaceDataPointer, bdata, 0, size);
                    var retrieveParams = new RetrieveImageDataParams()
                                             {
                                                 RequestId = Request.RequestId, 
                                                 Data = bdata, 
                                                 Width = width, 
                                                 Height = height, 
                                                 Pitch = pitch
                                             };
                    var t = new Thread(RetrieveImageData);
                    t.Start(retrieveParams);
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            finally
            {
                Request = null;
            }
        }

        private unsafe int PresentExHook(
            IntPtr devicePtr, 
            Rectangle* pSourceRect, 
            Rectangle* pDestRect, 
            IntPtr hDestWindowOverride, 
            IntPtr pDirtyRegion, 
            Present dwFlags)
        {
            int hresult = Result.Ok.Code;
            var device = (DeviceEx)devicePtr;
            if (!hooksStarted)
            {
                hresult = Direct3DDeviceEx_PresentExHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion, dwFlags);
                return hresult;
            }

            try
            {
                if (this.presentHookRecurse == 0)
                {
                    this.DoCaptureRenderTarget(device, "PresentEx");
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            finally
            {
                this.presentHookRecurse++;
                hresult = Direct3DDeviceEx_PresentExHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion, dwFlags);
                this.presentHookRecurse--;
            }
            return hresult;
        }

        private unsafe int PresentHook(
            IntPtr devicePtr, 
            Rectangle* pSourceRect, 
            Rectangle* pDestRect, 
            IntPtr hDestWindowOverride, 
            IntPtr pDirtyRegion)
        {
            int hresult;
            var device = (Device)devicePtr;
            if (!hooksStarted)
            {
                hresult = Direct3DDevice_PresentHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
                return hresult;
            }
            try
            {
                if (presentHookRecurse == 0)
                {
                    DoCaptureRenderTarget(device, "PresentHook");
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            finally
            {
                presentHookRecurse++;
                hresult = Direct3DDevice_PresentHook.Original(devicePtr, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
                presentHookRecurse--;
            }
            return hresult;
        }

        // private bool IsDeviceReady(Device device)
        // {
        // var cooplevel = device.TestCooperativeLevel();
        // if (cooplevel.Code != ResultCode.Success.Code)
        // {
        // return false;
        // }
        // return true;
        // }

        private int ResetExHook(IntPtr devicePtr, ref PresentParameters presentparameters, DisplayModeEx displayModeEx)
        {
            int hresult = Result.Ok.Code;
            DeviceEx device = (DeviceEx)devicePtr;
            try
            {
                if (!hooksStarted)
                {
                    hresult = Direct3DDeviceEx_ResetExHook.Original(devicePtr, ref presentparameters, displayModeEx);
                    return hresult;
                }

                ClearData();

                hresult = Direct3DDeviceEx_ResetExHook.Original(devicePtr, ref presentparameters, displayModeEx);

                if (currentDevice != devicePtr)
                {
                    hooksStarted = false;
                    currentDevice = devicePtr;
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            return hresult;
        }

        /// <summary>
        /// Reset the _renderTarget so that we are sure it will have the correct presentation parameters (required to support working across changes to windowed/fullscreen or resolution changes)
        /// </summary>
        /// <param name="devicePtr"></param>
        /// <param name="presentParameters"></param>
        /// <returns></returns>
        private int ResetHook(IntPtr devicePtr, ref PresentParameters presentParameters)
        {
            int hresult = Result.Ok.Code;
            Device device = (Device)devicePtr;
            try
            {
                if (!hooksStarted)
                {
                    hresult = Direct3DDevice_ResetHook.Original(devicePtr, ref presentParameters);
                    return hresult;
                }

                ClearData();

                hresult = Direct3DDevice_ResetHook.Original(devicePtr, ref presentParameters);

                if (currentDevice != devicePtr)
                {
                    hooksStarted = false;
                    currentDevice = devicePtr;
                }
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
            return hresult;
        }

        /// <summary>
        /// ParameterizedThreadStart method that places the image data from the stream into a byte array and then sets the Interface screenshot response. This can be called asynchronously.
        /// </summary>
        /// <param name="param">An instance of RetrieveImageDataParams is required to be passed as the parameter.</param>
        /// <remarks>The stream object passed will be closed!</remarks>
        private void RetrieveImageData(object param)
        {
            var retrieveParams = (RetrieveImageDataParams)param;
            ProcessCapture(retrieveParams);
        }

        private void SetupData(Device device)
        {
            DebugMessage("SetupData called");

            using (SwapChain swapChain = device.GetSwapChain(0))
            {
                PresentParameters pp = swapChain.PresentParameters;
                width = pp.BackBufferWidth;
                height = pp.BackBufferHeight;
                format = pp.BackBufferFormat;

                DebugMessage(string.Format("D3D9 Setup: w: {0} h: {1} f: {2}", width, height, format));
            }

            this.hooksStarted = true;
        }

        private void SetupSurfaces(Device device)
        {
            try
            {
                this.surface = Surface.CreateOffscreenPlain(device, width, height, (Format)format, Pool.SystemMemory);
                var lockedRect = this.surface.LockRectangle(LockFlags.ReadOnly);
                this.pitch = lockedRect.Pitch;
                this.surface.UnlockRectangle();
                this.renderTarget = Surface.CreateRenderTarget(device, width, height, format, MultisampleType.None, 0, false);
                this.query = new Query(device, QueryType.Event);
                surfacesSetup = true;
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
        }

        #endregion
    }
}