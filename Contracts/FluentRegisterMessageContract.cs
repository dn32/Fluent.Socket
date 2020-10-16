using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentRegisterMessageContract : FluentMessageContractBase
    {
        internal FluentRegisterMessageContract(object content) : base(content) { }
        protected FluentRegisterMessageContract() { }
    }
}