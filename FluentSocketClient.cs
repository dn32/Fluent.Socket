using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public class FluentSocketClient : FluentSocket
    {
        #region PROPERTIES

        public Uri Uri { get; set; }
        public string Url { get; set; }
        private new IFluentSocketClientEvents Events => base.Events as IFluentSocketClientEvents;
        public int ReconnectInterval { get; set; } = 2000;
        public int PingInterval { get; set; } = 5000;
        public new ClientWebSocket WebSocket => base.WebSocket as ClientWebSocket;

        #endregion

        private FluentSocketClient(IFluentSocketClientEvents events, string preIdentifier, string url) : base(preIdentifier)
        {
            base.Events = events;
            base.WebSocket = new ClientWebSocket();
            Url = url;
            Url += Url.Contains("?") ? "&" : "?";
            Url += $"SocketId={WebUtility.UrlEncode(SocketId)}";
            Uri = new Uri(Url);
        }

        public static void Initialize(string url, IFluentSocketClientEvents events, string preIdentifier, CancellationToken cancellationToken)
        {
            var instance = new FluentSocketClient(events, preIdentifier, url)
            {
                CancellationToken = cancellationToken,
            };

            _ = instance.ConectSocket();
            _ = instance.StartReceivingData();
        }

        private async Task ConectSocket()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (WebSocket.State == WebSocketState.Open)
                    {
                        await Task.Delay(PingInterval);
                        await PingServer(this);
                        Events.PingOk(this);
                        continue;
                    }
                    else
                    {
                        Channels.MyServer = null;
                        WebSocket?.Dispose();
                        base.WebSocket = new ClientWebSocket();
                    }

                    Events.Connecting(this);
                    await WebSocket.ConnectAsync(Uri, CancellationToken);

                    var clientData = Events.GetClientData();
                    var content = ClientIdentityOperations.GetClientData();
                    content.ClientData = clientData;

                    await this.SendData(new FluentMessageContract { Content = content, MessageType = EnumMessageType.REGISTER }, CancellationToken);
                    await Events.ConnectedAsync(this);
                }
                catch (WebSocketException ex)
                {
                    Channels.MyServer = null;
                    Events.LossOfConnection(this, ex.Message);
                    await Task.Delay(ReconnectInterval);
                    WebSocket.Dispose();
                }
            }
        }

        private async Task StartReceivingData()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await ReceiveFromServerData();
                    if (message != null)
                    {
                        if (message.MessageType == EnumMessageType.REQUIRED_INFORMATION)
                        {
                            Channels.MyServer = new Tuple<string, FluentSocket>(message.Sender.Last(), this);
                            await Events.InitialInformationReceived(this, message);
                        }
                        else
                        {
                            await ByPassAsync(message);
                        }
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(ReconnectInterval);
                    continue;
                }
            }
        }

        private async Task<FluentMessageContract> ReceiveFromServerData()
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open) return null;

            using var ms = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult result;
            do
            {
                result = await WebSocket.ReceiveAsync(buffer, CancellationToken);
                await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            return Util.DeserializeFromStream<FluentMessageContract>(ms);
        }

        #region DISPOSE

        private IntPtr nativeResource = Marshal.AllocHGlobal(100);

        ~FluentSocketClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                WebSocket?.Dispose();
            }

            if (nativeResource != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(nativeResource);
                nativeResource = IntPtr.Zero;
            }
        }

        #endregion
    }
}