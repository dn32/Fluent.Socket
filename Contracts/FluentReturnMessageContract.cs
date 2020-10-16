using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentReturnMessageContract : FluentMessageContractBase
    {
        public string OriginalRequestId { get; set; }
        internal FluentReturnMessageContract(object content): base(content) { }
        protected FluentReturnMessageContract() { }
    }
}