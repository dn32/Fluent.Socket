using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentWaitMessageContract : FluentMessageContractBase
    {
        internal FluentWaitMessageContract(object content) : base(content) { }
        protected FluentWaitMessageContract() { }
    }
}