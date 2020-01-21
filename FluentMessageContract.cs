using System;


namespace Fluent.Socket
{
    [Serializable]
    public class FluentMessageContract
    {
        public object Content { get; set; }
        public EnumMessageType MessageType { get; set; } = EnumMessageType.DATA;
    }

    [Serializable]
    public enum EnumMessageType
    {
        DATA = 0,
        PING = 1,
        REQUEST = 2,
        RETURN = 3
    }
}
