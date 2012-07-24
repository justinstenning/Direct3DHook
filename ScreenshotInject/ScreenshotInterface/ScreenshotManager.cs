using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScreenshotInterface
{
    public enum ResponseStatus
    {
        Complete,
        ReplacedWithNewRequest,
    }
    public delegate void ScreenshotRequestResponseNotification(Int32 clientPID, ResponseStatus status, ScreenshotResponse screenshotResponse);

    public delegate void ScreenshotDebugMessage(Int32 clientPID, string message);

    /// <summary>
    /// Static class that takes care of Screenshots requests and responses
    /// </summary>
    public static class ScreenshotManager
    {
        static Dictionary<Int32, ScreenshotRequestResponseNotification> _screenshotRequestNotifications = new Dictionary<int, ScreenshotRequestResponseNotification>();
        static Dictionary<Int32, ScreenshotRequest> _screenshotRequestByClientPID = new Dictionary<int, ScreenshotRequest>();

        /// <summary>
        /// An event representing a debug message
        /// </summary>
        public static event ScreenshotDebugMessage OnScreenshotDebugMessage;

        /// <summary>
        /// Add a debug message
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="message"></param>
        public static void AddScreenshotDebugMessage(Int32 clientPID, string message)
        {
            if (OnScreenshotDebugMessage != null)
            {
                OnScreenshotDebugMessage(clientPID, message);
            }
        }

        /// <summary>
        /// Add a screenshot request
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="screenshotRequest"></param>
        /// <param name="responseNotification"></param>
        public static void AddScreenshotRequest(Int32 clientPID, ScreenshotRequest screenshotRequest, ScreenshotRequestResponseNotification responseNotification)
        {
            try
            {
                if (!HookManager.HookedProcesses.Contains(clientPID))
                {
                    throw new ArgumentException("The client process must be hooked first", "clientPID");
                }
                if (screenshotRequest == null)
                {
                    throw new ArgumentNullException("screenshotRequest");
                }
                if (responseNotification == null)
                {
                    throw new ArgumentNullException("responseNotification");
                }

                lock (_screenshotRequestByClientPID)
                {
                    lock (_screenshotRequestNotifications)
                    {
                        _screenshotRequestByClientPID[clientPID] = screenshotRequest;
                        _screenshotRequestNotifications[clientPID] = responseNotification;
                    }
                }
            }
            catch
            {
            }
        }

        public static ScreenshotResponse GetScreenshotSynchronous(Int32 clientPID, ScreenshotRequest screenshotRequest)
        {
            // 2 second timeout
            return GetScreenshotSynchronous(clientPID, screenshotRequest, new TimeSpan(0, 0, 2));
        }

        public static ScreenshotResponse GetScreenshotSynchronous(Int32 clientPID, ScreenshotRequest screenshotRequest, TimeSpan timeout)
        {
            object srLock = new object();
            ScreenshotResponse sr = null;
            
            ScreenshotRequestResponseNotification srrn = delegate(Int32 cPID, ResponseStatus status, ScreenshotResponse screenshotResponse)
            {
                Interlocked.Exchange(ref sr, screenshotResponse);
            };
            DateTime startRequest = DateTime.Now;
            AddScreenshotRequest(clientPID, screenshotRequest, srrn);

            // Wait until the response has been returned
            while (true)
            {
                Thread.Sleep(10);

                
                if (sr != null)
                {
                    break;
                }
                
                // Break if timed out (will result in a null return value)
                if (DateTime.Now - startRequest > timeout)
                {
                    return null;
                }
            }

            return sr;
        }

        /// <summary>
        /// Get the screenshot request for the provided process Id (if the request exists)
        /// </summary>
        /// <param name="clientPID"></param>
        /// <returns></returns>
        public static ScreenshotRequest GetScreenshotRequest(Int32 clientPID)
        {
            try
            {
                lock (_screenshotRequestByClientPID)
                {
                    ScreenshotRequest result;

                    _screenshotRequestByClientPID.TryGetValue(clientPID, out result);
                    _screenshotRequestByClientPID.Remove(clientPID);

                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Set the screenshot response for the given process Id
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="screenshotResponse"></param>
        public static void SetScreenshotResponse(Int32 clientPID, ScreenshotResponse screenshotResponse)
        {
            try
            {
                lock (_screenshotRequestNotifications)
                {
                    if (_screenshotRequestNotifications[clientPID] != null)
                    {
                        _screenshotRequestNotifications[clientPID](clientPID, ResponseStatus.Complete, screenshotResponse);
                        
                        _screenshotRequestNotifications.Remove(clientPID);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
