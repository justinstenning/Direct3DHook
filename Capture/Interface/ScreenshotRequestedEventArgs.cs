using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Capture.Interface
{
    [Serializable]
    public class ScreenshotRequest: MarshalByRefObject
    {
        public Guid RequestId { get; set; }
        public Rectangle RegionToCapture { get; set; }

        public ScreenshotRequest(Rectangle region): this(Guid.NewGuid(), region)
        {
        }

        public ScreenshotRequest(Guid requestId, Rectangle region)
        {
            RequestId = requestId;
            RegionToCapture = region;
        }
    }
}
