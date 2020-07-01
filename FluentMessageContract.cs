using System;

namespace Fluent.Socket
{

    [Serializable]
    internal class FluentMessageContract
    {
        public object Content { get; set; }
        public bool IsReturn { get; set; }
        public bool IsRegister { get; set; }
    }
}