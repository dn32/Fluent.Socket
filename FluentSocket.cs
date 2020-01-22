using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

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
            SocketId = $"{preIdentifier}_{Guid.NewGuid().ToString()}";
        }

        public async Task PingServer(FluentSocketClient fluentSocketClient)
        {
            //await fluentSocketClient.SendData(new FluentMessageContract { MessageType = EnumMessageType.PING }, CancellationToken);
        }

        public async Task ByPassAsync(FluentMessageContract message)
        {
        next:
            var bypass = message.Bypass.Next();
            if (bypass == SocketId && message.Bypass.Count != 0)
            {
                goto next;
            }

            if (bypass == null)
            {
                if (message.MessageType == EnumMessageType.PING)
                {
                    var fluentSocketServer = this as FluentSocketServer;
                    fluentSocketServer.Events.PingReceived(fluentSocketServer, message);
                }
                else
                {
                    await Events.DataReceived(this, message);
                }
            }
            else
            {
                if (Channels.OnlineChannels.TryGetValue(bypass, out FluentSocket value))
                {
                    await value.SendData(message, CancellationToken);
                }
                else if (Channels.MyServer?.Item1 == bypass)
                {
                    await Channels.MyServer?.Item2.SendData(message, CancellationToken);
                }
                else
                {
                    await this.SendData(new FluentMessageContract { Content = $"The device {bypass} is not currently available", IsReturn = true, MessageType = EnumMessageType.ERROR }, CancellationToken);
                }
            }
        }
    }
}