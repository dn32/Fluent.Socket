using System;


namespace Fluent.Socket
{
    [Serializable]
    public enum EnumMessageType
    {
        DATA = 0,
        PING = 1,
        REQUEST = 2,
        RETURN = 3
    }
}
