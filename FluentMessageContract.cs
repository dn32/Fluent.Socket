using System;

namespace Fluent.Socket
{
    [Serializable]
    public class FluentMessageContract
    {
        public object Content { get; set; }
        public EnumMessageType MessageType { get; set; }
    }
}
