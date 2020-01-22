using System;
using System.Collections.Generic;

namespace Fluent.Socket
{
    [Serializable]
    public class FluentMessageContract
    {
        public FluentMessageContract()
        {
            Bypass = new List<string>();
            Sender = new List<string>();
        }

        public object Content { get; set; }
        public EnumMessageType MessageType { get; set; }
        public bool IsReturn { get; set; }
        public List<string> Bypass { get; set; }
        public List<string> Sender { get; private set; }
    }
}
