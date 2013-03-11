using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Capture.Hook.Common
{
    public class ImageElement
    {
        public System.IO.Stream ImageStream { get; set; }
        public float Alpha { get; set; }
        public System.Drawing.PointF Location { get; set; }
    }
}
