using System;
using System.Net.WebSockets;
using System.Threading;

namespace Fluent.Socket
{
    public class FluentSocket
    {
        #region PROPERTIES

        public WebSocket WebSocket { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string SocketId { get; set; }
        public virtual IFluentSocketEvents Events { get; set; }

        #endregion

        public FluentSocket(string preIdentifier)
        {
            SocketId = $"{preIdentifier}_{Guid.NewGuid()}";
        }
    }
}