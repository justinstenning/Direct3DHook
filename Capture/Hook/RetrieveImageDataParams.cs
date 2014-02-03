namespace Capture.Hook
{
    using System;

    /// <summary>
    /// Used to hold the parameters to be passed to RetrieveImageData
    /// </summary>
    public struct RetrieveImageDataParams
    {
        public Guid RequestId { get; set; }

        public byte[] Data { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public int Pitch { get; set; }
    }
}