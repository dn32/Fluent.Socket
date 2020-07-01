using System;

namespace Fluent.Socket {

    [Serializable]
    public class FluentMessageContract {
        public object Content { get; set; }
        public bool IsReturn { get; set; }
        public bool IsRegister { get; set; }
    }
}