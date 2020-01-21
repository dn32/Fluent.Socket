using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public class FluentSocketClient
    {
        #region PROPERTIES

        private ClientWebSocket ClientWebSocket { get; set; }
        private CancellationToken CancellationToken { get; set; }
        private Uri Uri { get; set; }
        private IFluentSocketClientEvents Events { get; set; }
        public int ReconnectInterval { get; set; } = 2000;
        public int PingInterval { get; set; } = 5000;

        #endregion

        private FluentSocketClient() { }

        public static void Initialize(Uri uri, IFluentSocketClientEvents events, CancellationToken cancellationToken)
        {
            var instance = new FluentSocketClient
            {
                Uri = uri,
                Events = events,
                CancellationToken = cancellationToken,
                ClientWebSocket = new ClientWebSocket()
            };

            _ = instance.ConectarSocket();
            _ = instance.StartReceivingData();
        }

        public async Task SendData(object obj)
        {
            var data = Util.ObjectToByteArray(new FluentMessageContract { Content = obj });
            await ClientWebSocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Binary, true, CancellationToken);
        }

        private async Task ConectarSocket()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (ClientWebSocket.State == WebSocketState.Open)
                    {
                        await Task.Delay(PingInterval);
                        await PingServer();
                        Events.PingOk(this);
                        continue;
                    }
                    else
                    {
                        ClientWebSocket?.Dispose();
                        ClientWebSocket = new ClientWebSocket();
                    }

                    Events.Connecting(this);
                    await ClientWebSocket.ConnectAsync(Uri, CancellationToken);
                    Events.Connected(this);
                }
                catch (WebSocketException ex)
                {
                    Events.LossOfConnection(this, ex.Message);
                    await Task.Delay(ReconnectInterval);
                    ClientWebSocket.Dispose();
                }
            }
        }

        private async Task StartReceivingData()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await ReceiveServerData();
                    if (result != null) Events.DataReceived(this, result);
                }
                catch (Exception)
                {
                    await Task.Delay(ReconnectInterval);
                    continue;
                }
            }
        }

        private async Task PingServer()
        {
            await SendData(new FluentMessageContract { MessageType = EnumMessageType.PING });
        }

        private async Task<object> ReceiveServerData()
        {
            if (ClientWebSocket == null || ClientWebSocket.State != WebSocketState.Open) return null;

            using var ms = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult result;
            do
            {
                result = await ClientWebSocket.ReceiveAsync(buffer, CancellationToken);
                await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            return Util.DeserializeFromStream<FluentMessageContract>(ms)?.Content ?? null;
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
                ClientWebSocket?.Dispose();
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