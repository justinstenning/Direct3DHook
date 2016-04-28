using System.Collections.Generic;

namespace Capture.Hook.Common
{
    public class Overlay: IOverlay
    {
        public virtual List<IOverlayElement> Elements { get; set; } = new List<IOverlayElement>();

        public virtual bool Hidden { get; set; }

        public virtual void Frame()
        {
            foreach (var element in Elements)
            {
                element.Frame();
            }
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
