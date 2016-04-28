using System;
using System.IO;
using System.Reflection;

namespace Capture.Interface
{
    [Serializable]
    public class CaptureConfig
    {
        public Direct3DVersion Direct3DVersion { get; set; }
        public bool ShowOverlay { get; set; }
        public int TargetFramesPerSecond { get; }
        public string TargetFolder { get; set; }

        public CaptureConfig()
        {
            Direct3DVersion = Direct3DVersion.AutoDetect;
            ShowOverlay = false;
            TargetFramesPerSecond = 5;
            TargetFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
