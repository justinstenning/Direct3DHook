namespace Capture.Interface
{
    using System;

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
            if (this.RecordingStarted != null)
                this.RecordingStarted(config);
        }

        public void RecordingStoppedProxyHandler()
        {
            if (this.RecordingStopped != null)
                this.RecordingStopped();
        }


        public void DisconnectedProxyHandler()
        {
            if (this.Disconnected != null)
                this.Disconnected();
        }

        public void ScreenshotRequestedProxyHandler(ScreenshotRequest request)
        {
            if (this.ScreenshotRequested != null)
                this.ScreenshotRequested(request);
        }

        public void DisplayTextProxyHandler(DisplayTextEventArgs args)
        {
            if (this.DisplayText != null)
                this.DisplayText(args);
        }
    }
}