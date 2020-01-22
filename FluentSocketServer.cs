using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public class FluentSocketServer : FluentSocket, IDisposable
    {
        public new IFluentSocketServerEvents Events => base.Events as IFluentSocketServerEvents;

        public HttpContext HttpContext { get; set; }

        public FluentSocketServer(HttpContext context, IFluentSocketServerEvents events, string preIdentifier) : base(preIdentifier)
        {
            base.Events = events;
            CancellationToken = new CancellationTokenSource().Token;
            HttpContext = context;
        }

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

                    var message = Util.ByteArrayToObject<FluentMessageContract>(ms.ToArray());

                    await ReceivedMessageFromClientAsync(message);
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

        private async Task ReceivedMessageFromClientAsync(FluentMessageContract message)
        {
            if (message.MessageType == EnumMessageType.REQUEST_INFO)
            {
                await this.SendData(new FluentMessageContract { Content = Events.GetInitialInformation(this), MessageType = EnumMessageType.REQUIRED_INFORMATION }, CancellationToken);
            }
            else
            {
                await ByPassAsync(message);
            }
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