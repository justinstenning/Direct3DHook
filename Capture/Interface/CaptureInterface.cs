using System;
using System.Drawing;
using System.Threading;

namespace Capture.Interface
{
    [Serializable]
    public delegate void RecordingStartedEvent(CaptureConfig config);
    [Serializable]
    public delegate void RecordingStoppedEvent();
    [Serializable]
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);
    [Serializable]
    public delegate void ScreenshotReceivedEvent(ScreenshotReceivedEventArgs response);
    [Serializable]
    public delegate void DisconnectedEvent();
    [Serializable]
    public delegate void ScreenshotRequestedEvent(ScreenshotRequest request);
    [Serializable]
    public delegate void DisplayTextEvent(DisplayTextEventArgs args);

    [Serializable]
    public class CaptureInterface : MarshalByRefObject
    {
        /// <summary>
        /// The client process Id
        /// </summary>
        public int ProcessId { get; set; }

        #region Events

        #region Server-side Events
        
        /// <summary>
        /// Server event for sending debug and error information from the client to server
        /// </summary>
        public event MessageReceivedEvent RemoteMessage;
        
        /// <summary>
        /// Server event for receiving screenshot image data
        /// </summary>
        public event ScreenshotReceivedEvent ScreenshotReceived;
        
        #endregion

        #region Client-side Events
        
        /// <summary>
        /// Client event used to communicate to the client that it is time to start recording
        /// </summary>
        public event RecordingStartedEvent RecordingStarted;

        /// <summary>
        /// Client event used to communicate to the client that it is time to stop recording
        /// </summary>
        public event RecordingStoppedEvent RecordingStopped;

        /// <summary>
        /// Client event used to communicate to the client that it is time to create a screenshot
        /// </summary>
        public event ScreenshotRequestedEvent ScreenshotRequested;

        /// <summary>
        /// Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        /// <summary>
        /// Client event used to display a piece of text in-game
        /// </summary>
        public event DisplayTextEvent DisplayText;
        
        #endregion

        #endregion

        public bool IsRecording { get; set; }

        #region Public Methods

        #region Video Capture

        /// <summary>
        /// If not <see cref="IsRecording"/> will invoke the <see cref="RecordingStarted"/> event, starting a new recording. 
        /// </summary>
        /// <param name="config">The configuration for the recording</param>
        /// <remarks>Handlers in the server and remote process will be be invoked.</remarks>
        public void StartRecording(CaptureConfig config)
        {
            if (IsRecording)
                return;
            SafeInvokeRecordingStarted(config);
            IsRecording = true;
        }

        /// <summary>
        /// If <see cref="IsRecording"/>, will invoke the <see cref="RecordingStopped"/> event, finalising any existing recording.
        /// </summary>
        /// <remarks>Handlers in the server and remote process will be be invoked.</remarks>
        public void StopRecording()
        {
            if (!IsRecording)
                return;
            SafeInvokeRecordingStopped();
            IsRecording = false;
        }

        #endregion

        #region Still image Capture

        readonly object _lock = new object();
        Guid? _requestId;
        Action<Screenshot> _completeScreenshot;
        readonly ManualResetEvent _wait = new ManualResetEvent(false);

        /// <summary>
        /// Get a fullscreen screenshot with the default timeout of 2 seconds
        /// </summary>
        public Screenshot GetScreenshot()
        {
            return GetScreenshot(Rectangle.Empty, new TimeSpan(0, 0, 2), null, ImageFormat.Bitmap);
        }

        /// <summary>
        /// Get a screenshot of the specified region
        /// </summary>
        /// <param name="region">the region to capture (x=0,y=0 is top left corner)</param>
        /// <param name="timeout">maximum time to wait for the screenshot</param>
        public Screenshot GetScreenshot(Rectangle region, TimeSpan timeout, Size? resize, ImageFormat format)
        {
            lock (_lock)
            {
                Screenshot result = null;
                _requestId = Guid.NewGuid();
                _wait.Reset();

                SafeInvokeScreenshotRequested(new ScreenshotRequest(_requestId.Value, region)
                {
                    Format = format,
                    Resize = resize
                });

                _completeScreenshot = sc =>
                {
                    try
                    {
                        Interlocked.Exchange(ref result, sc);
                    }
                    catch
                    {
                    }
                    _wait.Set();
                        
                };

                _wait.WaitOne(timeout);
                _completeScreenshot = null;
                return result;
            }
        }

        public IAsyncResult BeginGetScreenshot(Rectangle region, TimeSpan timeout, AsyncCallback callback = null, Size? resize = null, ImageFormat format = ImageFormat.Bitmap)
        {
            Func<Rectangle, TimeSpan, Size?, ImageFormat, Screenshot> getScreenshot = GetScreenshot;
            
            return getScreenshot.BeginInvoke(region, timeout, resize, format, callback, getScreenshot);
        }

        public Screenshot EndGetScreenshot(IAsyncResult result)
        {
            var getScreenshot = result.AsyncState as Func<Rectangle, TimeSpan, Size?, ImageFormat, Screenshot>;
            return getScreenshot?.EndInvoke(result);
        }

        public void SendScreenshotResponse(Screenshot screenshot)
        {
            if (_requestId != null && screenshot != null && screenshot.RequestId == _requestId.Value)
            {
                _completeScreenshot?.Invoke(screenshot);
            }
        }

        #endregion

        /// <summary>
        /// Tell the client process to disconnect
        /// </summary>
        public void Disconnect()
        {
            SafeInvokeDisconnected();
        }

        /// <summary>
        /// Send a message to all handlers of <see cref="CaptureInterface.RemoteMessage"/>.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Message(MessageType messageType, string format, params object[] args)
        {
            Message(messageType, string.Format(format, args));
        }

        public void Message(MessageType messageType, string message)
        {
            SafeInvokeMessageRecevied(new MessageReceivedEventArgs(messageType, message));
        }

        /// <summary>
        /// Display text in-game for the default duration of 5 seconds
        /// </summary>
        /// <param name="text"></param>
        public void DisplayInGameText(string text)
        {
            DisplayInGameText(text, new TimeSpan(0, 0, 5));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="duration"></param>
        public void DisplayInGameText(string text, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
                throw new ArgumentException("Duration must be larger than 0", nameof(duration));
            SafeInvokeDisplayText(new DisplayTextEventArgs(text, duration));
        }

        #endregion

        #region Private: Invoke message handlers

        void SafeInvokeRecordingStarted(CaptureConfig config)
        {
            if (RecordingStarted == null)
                return;         //No Listeners

            RecordingStartedEvent listener = null;
            var dels = RecordingStarted.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (RecordingStartedEvent)del;
                    listener.Invoke(config);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    RecordingStarted -= listener;
                }
            }
        }

        void SafeInvokeRecordingStopped()
        {
            if (RecordingStopped == null)
                return;         //No Listeners

            RecordingStoppedEvent listener = null;
            var dels = RecordingStopped.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (RecordingStoppedEvent)del;
                    listener.Invoke();
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    RecordingStopped -= listener;
                }
            }
        }

        void SafeInvokeMessageRecevied(MessageReceivedEventArgs eventArgs)
        {
            if (RemoteMessage == null)
                return;         //No Listeners

            MessageReceivedEvent listener = null;
            var dels = RemoteMessage.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (MessageReceivedEvent)del;
                    listener.Invoke(eventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    RemoteMessage -= listener;
                }
            }
        }

        void SafeInvokeScreenshotRequested(ScreenshotRequest eventArgs)
        {
            if (ScreenshotRequested == null)
                return;         //No Listeners

            ScreenshotRequestedEvent listener = null;
            var dels = ScreenshotRequested.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (ScreenshotRequestedEvent)del;
                    listener.Invoke(eventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    ScreenshotRequested -= listener;
                }
            }
        }

        void SafeInvokeScreenshotReceived(ScreenshotReceivedEventArgs eventArgs)
        {
            if (ScreenshotReceived == null)
                return;         //No Listeners

            ScreenshotReceivedEvent listener = null;
            var dels = ScreenshotReceived.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (ScreenshotReceivedEvent)del;
                    listener.Invoke(eventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    ScreenshotReceived -= listener;
                }
            }
        }

        void SafeInvokeDisconnected()
        {
            if (Disconnected == null)
                return;         //No Listeners

            DisconnectedEvent listener = null;
            var dels = Disconnected.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (DisconnectedEvent)del;
                    listener.Invoke();
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    Disconnected -= listener;
                }
            }
        }

        void SafeInvokeDisplayText(DisplayTextEventArgs displayTextEventArgs)
        {
            if (DisplayText == null)
                return;         //No Listeners

            DisplayTextEvent listener = null;
            var dels = DisplayText.GetInvocationList();

            foreach (var del in dels)
            {
                try
                {
                    listener = (DisplayTextEvent)del;
                    listener.Invoke(displayTextEventArgs);
                }
                catch (Exception)
                {
                    //Could not reach the destination, so remove it
                    //from the list
                    DisplayText -= listener;
                }
            }
        }

        #endregion

        /// <summary>
        /// Used 
        /// </summary>
        public void Ping()
        {
            
        }
    }

    /// <summary>
    /// Client event proxy for marshalling event handlers
    /// </summary>
    public class ClientCaptureInterfaceEventProxy : MarshalByRefObject
    {
        #region Event Declarations

        /// <summary>
        /// Client event used to communicate to the client that it is time to start recording
        /// </summary>
        public event RecordingStartedEvent RecordingStarted;

        /// <summary>
        /// Client event used to communicate to the client that it is time to stop recording
        /// </summary>
        public event RecordingStoppedEvent RecordingStopped;

        /// <summary>
        /// Client event used to communicate to the client that it is time to create a screenshot
        /// </summary>
        public event ScreenshotRequestedEvent ScreenshotRequested;

        /// <summary>
        /// Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        /// <summary>
        /// Client event used to display in-game text
        /// </summary>
        public event DisplayTextEvent DisplayText;

        #endregion

        #region Lifetime Services

        public override object InitializeLifetimeService()
        {
            //Returning null holds the object alive
            //until it is explicitly destroyed
            return null;
        }

        #endregion

        public void RecordingStartedProxyHandler(CaptureConfig config)
        {
            RecordingStarted?.Invoke(config);
        }

        public void RecordingStoppedProxyHandler()
        {
            RecordingStopped?.Invoke();
        }


        public void DisconnectedProxyHandler()
        {
            Disconnected?.Invoke();
        }

        public void ScreenshotRequestedProxyHandler(ScreenshotRequest request)
        {
            ScreenshotRequested?.Invoke(request);
        }

        public void DisplayTextProxyHandler(DisplayTextEventArgs args)
        {
            DisplayText?.Invoke(args);
        }
    }
}
