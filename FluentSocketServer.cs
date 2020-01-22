using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public class FluentSocketServer : IDisposable
    {
        private WebSocket WebSocket { get; set; }
        public HttpContext HttpContext { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string SocketId { get; set; }

        public FluentSocketServer(HttpContext context)
        {
            CancellationToken = new CancellationTokenSource().Token;
            HttpContext = context;
            SocketId = Guid.NewGuid().ToString();
        }

        #region ACTIONS

        internal Action<FluentSocketServer, object> DataReceived { get; set; } = (FluentSocketServer fluentSocketServer, object obj) => Console.WriteLine("Data received!");

        internal Action<FluentSocketServer, object> PingReceived { get; set; } = (FluentSocketServer fluentSocketServer, object obj) => Console.WriteLine("Ping received!");

        #endregion

        public async Task InitAsync(Action<FluentSocketServer> connected)
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            WebSocketReceiveResult result = null;
            connected(this);

            do
            {
                try
                {
                    using var ms = new MemoryStream();
                    var buffer = new ArraySegment<byte>(new byte[8192]);
                    do
                    {
                        result = await WebSocket.ReceiveAsync(buffer, CancellationToken);
                        if (result.CloseStatus.HasValue) { break; }
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);
                    ms.Seek(0, SeekOrigin.Begin);

                    var obj = Util.ByteArrayToObject<FluentMessageContract>(ms.ToArray());
                    if(obj.MessageType == EnumMessageType.PING)
                    {
                        PingReceived(this, obj?.Content);
                    }
                    else
                    {
                        DataReceived(this, obj?.Content);
                    }
                }
                catch (WebSocketException)
                {
                    if (WebSocket.State == WebSocketState.Open)
                    {
                        WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "", CancellationToken).Wait();
                    }

                    WebSocket.Dispose();
                    Dispose();
                    break;
                }
            }
            while (!result.CloseStatus.HasValue && !CancellationToken.IsCancellationRequested);
        }

        public async Task SendData(FluentMessageContract fluentMessageContract)
        {
            var data = Util.ObjectToByteArray(fluentMessageContract);
            await WebSocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Binary, true, CancellationToken);
        }

        #region DISPOSE

        private IntPtr nativeResource = Marshal.AllocHGlobal(100);

        ~FluentSocketServer()
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