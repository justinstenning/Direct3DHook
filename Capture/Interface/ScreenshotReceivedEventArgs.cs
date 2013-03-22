using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Capture.Interface
{
    [Serializable]
    public class ScreenshotReceivedEventArgs: MarshalByRefObject
    {
        public Int32 ProcessId { get; set; }
        public byte[][] Screenshot { get; set; }

        public Guid RequestId { get; set; }

        public ScreenshotReceivedEventArgs(Int32 processId, byte[][] screenshot, Guid requestId)
        {
            ProcessId = processId;
            Screenshot = screenshot;
            RequestId = requestId;
        }
    }
}
