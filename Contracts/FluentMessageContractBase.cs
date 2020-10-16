using System;

namespace Fluent.Socket.Contracts
{
    [Serializable]
    internal class FluentMessageContractBase
    {
        public string RequestId { get; private set; }

        public object Content { get; set; }

        internal FluentMessageContractBase(object content)
        {
            RequestId = Guid.NewGuid().ToString();
            Content = content;
        }

        protected FluentMessageContractBase()
        { 
            RequestId = Guid.NewGuid().ToString();
        }
    }
}