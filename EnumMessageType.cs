using System;


namespace Fluent.Socket
{
    [Serializable]
    public enum EnumMessageType
    {
        DATA = 0,
        PING = 1,
        SEND = 2,
        RETURN = 3,
        WARNING = 4,
        ERROR = 5,
        DEVICE_NOT_FOUND = 5,
        REGISTER = 6,
        UNREGISTER = 7,
        SUCCESSFULLY_REGISTERED = 8,
        REQUEST_INFO = 9,
        REQUIRED_INFORMATION = 10
    }
}
