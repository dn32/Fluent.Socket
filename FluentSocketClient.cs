using System;
using System.IO;
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

        public async Task CloseConnectionAsync()
        {
            try
            {
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken());
            }
            catch { }

            WebSocket?.Dispose();
        }

        public static void Initialize(string url, IFluentSocketClientEvents events, string preIdentifier, CancellationToken cancellationToken)
        {
            var instance = new FluentSocketClient(events, preIdentifier, url)
            {
                CancellationToken = cancellationToken,
            };

            events.Initialize(instance, instance.SocketId);

            _ = instance.ConectSocket();
            _ = instance.StartReceivingData();
        }

        private async Task ConectSocket()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (base.WebSocket.State == WebSocketState.Open)
                    {
                        await Task.Delay(PingInterval);
                        continue;
                    }
                    else
                    {
                        Channels.MyServer = null;
                        base.WebSocket?.Dispose();
                        base.WebSocket = new ClientWebSocket();
                    }
                    Events.Connecting();
                    await ((ClientWebSocket)base.WebSocket).ConnectAsync(Uri, CancellationToken);

                    var clientData = Events.GetClientData();
                    await this.SendInternalData(new FluentMessageContract { Content = clientData, IsRegister = true }, CancellationToken);
                    Events.Connected();
                }
                catch (WebSocketException ex)
                {
                    Channels.MyServer = null;
                    Events.LossOfConnection(ex.Message);
                    await Task.Delay(ReconnectInterval);
                    base.WebSocket.Dispose();
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
                    if (message == null)
                        await Task.Delay(200);
                    else
                        await Events.DataReceived(message.Content);

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