using System;
using System.Drawing;

namespace Capture.Hook.Common
{
    public class FramesPerSecond: TextElement
    {
        string _fpsFormat = "{0:N0} fps";
        public override string Text
        {
            get { return string.Format(_fpsFormat, GetFPS()); }
            set { _fpsFormat = value; }
        }

        int _frames;
        int _lastTickCount;
        float _lastFrameRate;

        public FramesPerSecond(Font font)
            : base(font)
        {
        }

        /// <summary>
        /// Must be called each frame
        /// </summary>
        public override void Frame()
        {
            _frames++;

            if (Math.Abs(Environment.TickCount - _lastTickCount) <= 1000)
                return;

            _lastFrameRate = (float)_frames * 1000 / Math.Abs(Environment.TickCount - _lastTickCount);
            _lastTickCount = Environment.TickCount;
            _frames = 0;
        }

        /// <summary>
        /// Return the current frames per second
        /// </summary>
        /// <returns></returns>
        public float GetFPS() => _lastFrameRate;
    }
}
