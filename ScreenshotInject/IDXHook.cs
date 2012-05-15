using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScreenshotInterface;

namespace ScreenshotInject
{
    internal interface IDXHook
    {
        ScreenshotInterface.ScreenshotInterface Interface
        {
            get;
            set;
        }

        bool ShowOverlay
        {
            get;
            set;
        }

        ScreenshotRequest Request
        {
            get;
            set;
        }

        void Hook();

        void Cleanup();
    }
}
