using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentWaitMessageContractRegister : FluentMessageContractBase
    {
        internal FluentWaitMessageContractRegister(object content) : base(content) { }
        protected FluentWaitMessageContractRegister() { }
    }
}