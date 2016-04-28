using System.Collections.Generic;

namespace Capture.Hook.Common
{
    interface IOverlay: IOverlayElement
    {
        List<IOverlayElement> Elements { get; set; }
    }
}
