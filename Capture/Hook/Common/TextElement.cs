using System.Drawing;

namespace Capture.Hook.Common
{
    public class TextElement: Element
    {
        public virtual string Text { get; set; }
        public virtual Font Font { get; }
        public virtual Color Color { get; set; }
        public virtual Point Location { get; set; }
        public virtual bool AntiAliased { get; set; }

        public TextElement(Font font)
        {
            Font = font;
        }
    }
}
