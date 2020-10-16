using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentDefaultMessageContract: FluentMessageContractBase
    {
        internal FluentDefaultMessageContract(object content) : base(content) { }
        protected FluentDefaultMessageContract() { }
    }
}