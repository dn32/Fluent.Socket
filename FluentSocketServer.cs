using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluent.Socket.Contracts;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Fluent.Socket
{
    public class FluentSocketServer : FluentSocket, IDisposable
    {
        public new IFluentSocketServerEvents Events => base.Events as IFluentSocketServerEvents;

        public HttpContext HttpContext { get; set; }

        public FluentSocketServer(HttpContext context, IFluentSocketServerEvents events, string preIdentifier, bool useJson = false) : base(preIdentifier)
        {
            base.Events = events;
            CancellationToken = new CancellationTokenSource().Token;
            HttpContext = context;
            UseJson = useJson;
        }

        public async Task CloseConnectionAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            try
            {
                await WebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
                //await WebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
            }
            catch { }

            WebSocket?.Dispose();
        }

        public async Task InitAsync(HttpContext httpContext, Action<FluentSocketServer> connected)
        {
            WebSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
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

                    FluentMessageContractBase message;

                    if (UseJson)
                    {
                        var json = Encoding.ASCII.GetString(ms.ToArray());
                        if (string.IsNullOrWhiteSpace(json)) return;
                        message = JsonConvert.DeserializeObject<FluentMessageContractBase>(json);
                    }
                    else
                    {
                        message = Util.ByteArrayToObject<FluentMessageContractBase>(ms.ToArray());
                    }

                    await ReceivedMessageFromClientAsync(message);
                }
                catch (WebSocketException)
                {
                    if (WebSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "", CancellationToken);
                        }
                        catch { }
                    }

                    await Events.ClientDisconnected();
                    WebSocket?.Dispose();
                    Dispose();
                    break;
                }
            }
            while (!result.CloseStatus.HasValue && !CancellationToken.IsCancellationRequested);
        }

        private async Task ReceivedMessageFromClientAsync(FluentMessageContractBase message)
        {
            if (message is FluentRegisterMessageContract register)
            {
                await Events.DataReceivedRegister(register.Content);
            }
            else if (message is FluentWaitMessageContract waitMesssage)
            {
                var returnContent = await Events.DataReceivedAndWait(waitMesssage.Content);
                var json = JsonConvert.SerializeObject(returnContent);
                var returnContract = new FluentReturnMessageContract(json) { OriginalRequestId = waitMesssage.RequestId };
                await this.SendInternalData(returnContract, CancellationToken);
            }
            else if (message is FluentDefaultMessageContract defaultMessage)
            {
                await Events.DataReceived(defaultMessage.Content);
            }
            else if (message is FluentReturnMessageContract returnMessage)
            {
                await FluentSocketUtil.ReturnReceivedFromClient(returnMessage);
            }
        }

        #region DISPOSE

        public bool Disposed { get; set; } = false;

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
            Disposed = true;

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