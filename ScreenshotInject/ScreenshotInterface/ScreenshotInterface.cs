using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using EasyHook;
using System.Drawing;
using System.IO;
using System.Threading;

namespace ScreenshotInterface
{
    public enum Direct3DVersion
    {
        Unknown,
        AutoDetect,
        Direct3D9,
        Direct3D10,
        Direct3D10_1,
        Direct3D11,
        Direct3D11_1,
    }

    public class ScreenshotRequest : MarshalByRefObject
    {
        public ScreenshotRequest(Rectangle regionToCapture)
        {
            _regionToCapture = regionToCapture;
        }

        Guid _requestId = Guid.NewGuid();
        Rectangle _regionToCapture;

        public Guid RequestId
        {
            get
            {
                return _requestId;
            }
        }

        public Rectangle RegionToCapture
        {
            get
            {
                return _regionToCapture;
            }
        }
    }

    public class ScreenshotResponse : MarshalByRefObject
    {
        public ScreenshotResponse(Guid requestId, byte[] capturedBitmap)
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

        public Bitmap CapturedBitmapAsImage
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(_capturedBitmap))
                {
                    return (Bitmap)Image.FromStream(ms);
                }
            }
        }
    }

    public class ScreenshotInterface : MarshalByRefObject
    {
        public void ReportError(Int32 clientPID, Exception e)
        {
            OnDebugMessage(clientPID, "A client process (" + clientPID + ") has reported an error\r\n" + e.Message);
            //MessageBox.Show(e.ToString(), "A client process (" + clientPID + ") has reported an error...", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        public bool Ping(Int32 clientPID)
        {
            /*
             * We should just check if the client is still in our list
             * of hooked processes...
             */
            lock (HookManager.ProcessList)
            {
                return HookManager.HookedProcesses.Contains(clientPID);
            }
        }

        public ScreenshotRequest GetScreenshotRequest(Int32 clientPID)
        {
            return ScreenshotManager.GetScreenshotRequest(clientPID);
        }


        private class RequestNotificationThreadParameter
        {
            public Int32 ClientPID;
            public ScreenshotResponse Response;
        }

        private void ProcessResponseThread(object data)
        {
            RequestNotificationThreadParameter responseData = (RequestNotificationThreadParameter)data;
            ScreenshotManager.SetScreenshotResponse(responseData.ClientPID, responseData.Response);
        }

        public void OnScreenshotResponse(Int32 clientPID, Guid requestId, byte[] bitmapData)
        {
            //using (MemoryStream ms = new MemoryStream(bitmapData))
            //{
            //    using (Bitmap bm = (Bitmap)Image.FromStream(ms))
            //    {
            //    }
            //}
            Thread t = new Thread(new ParameterizedThreadStart(ProcessResponseThread));
            t.Start(new RequestNotificationThreadParameter() { ClientPID = clientPID, Response = new ScreenshotResponse(requestId, bitmapData) });
        }

        public void OnDebugMessage(Int32 clientPID, string message)
        {
            ScreenshotManager.AddScreenshotDebugMessage(clientPID, message);
        }

    }
}
