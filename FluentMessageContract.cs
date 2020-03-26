using System;
using System.Collections.Generic;

namespace Fluent.Socket
{
    [Serializable]
    public class FluentMessageContract
    {
        public FluentMessageContract() { }

        public FluentMessageContract(EnumMessageType messageType) 
        {
            MessageType = messageType;
        }

        public object Content { get; set; }
        public EnumMessageType MessageType { get; set; }
        public bool IsReturn { get; set; }
    }
}
