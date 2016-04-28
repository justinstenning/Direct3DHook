using System;

namespace Capture.Interface
{
    [Serializable]
    public class DisplayTextEventArgs: MarshalByRefObject
    {
        public string Text { get; }
        public TimeSpan Duration { get; }

        public DisplayTextEventArgs(string text, TimeSpan duration)
        {
            Text = text;
            Duration = duration;
        }

        public override string ToString() => Text;
    }
}
