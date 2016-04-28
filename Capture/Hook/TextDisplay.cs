using System;

namespace Capture.Hook
{
    public class TextDisplay
    {
        readonly long _startTickCount;

        public TextDisplay()
        {
            _startTickCount = DateTime.Now.Ticks;
            Display = true;
        }

        /// <summary>
        /// Must be called each frame
        /// </summary>
        public void Frame()
        {
            if (Display && Math.Abs(DateTime.Now.Ticks - _startTickCount) > Duration.Ticks)
            {
                Display = false;
            }
        }

        public bool Display { get; set; }
        public string Text { get; set; }
        public TimeSpan Duration { get; set; }
        public float Remaining
        {
            get
            {
                if (Display)
                {
                    return Math.Abs(DateTime.Now.Ticks - _startTickCount) / (float)Duration.Ticks;
                }
                return 0;
            }
        }
    }
}
