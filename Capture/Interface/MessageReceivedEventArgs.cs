using System;

namespace Capture.Interface
{
    [Serializable]   
    public class MessageReceivedEventArgs: MarshalByRefObject
    {
        public MessageType MessageType { get; }
        public string Message { get; }

        public MessageReceivedEventArgs(MessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString() => $"{MessageType}: {Message}";
    }
}