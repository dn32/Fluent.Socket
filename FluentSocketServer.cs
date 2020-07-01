﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fluent.Socket {
    public class FluentSocketServer : FluentSocket, IDisposable {
        public new IFluentSocketServerEvents Events => base.Events as IFluentSocketServerEvents;

        public HttpContext HttpContext { get; set; }

        public FluentSocketServer (HttpContext context, IFluentSocketServerEvents events, string preIdentifier) : base (preIdentifier) {
            base.Events = events;
            CancellationToken = new CancellationTokenSource ().Token;
            HttpContext = context;
        }

        public async Task CloseConnectionAsync (WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            try {
                await WebSocket.CloseOutputAsync (closeStatus, statusDescription, cancellationToken);
                //await WebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
            } catch { }

            WebSocket?.Dispose ();
        }

        public async Task InitAsync (HttpContext httpContext, Action<FluentSocketServer> connected) {
            WebSocket = await httpContext.WebSockets.AcceptWebSocketAsync ();
            WebSocketReceiveResult result = null;

            connected (this);

            do {
                try {
                    using var ms = new MemoryStream ();
                    var buffer = new ArraySegment<byte> (new byte[8192]);
                    do {
                        result = await WebSocket.ReceiveAsync (buffer, CancellationToken);
                        if (result.CloseStatus.HasValue) { break; }
                        ms.Write (buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);
                    ms.Seek (0, SeekOrigin.Begin);

                    var message = Util.ByteArrayToObject<FluentMessageContract> (ms.ToArray ());

                    await ReceivedMessageFromClientAsync (message);
                } catch (WebSocketException) {
                    if (WebSocket.State == WebSocketState.Open) {
                        try {
                            WebSocket.CloseAsync (WebSocketCloseStatus.InternalServerError, "", CancellationToken).Wait ();
                        } catch { }
                    }

                    Events.ClientDisconnected ();
                    WebSocket?.Dispose ();
                    Dispose ();
                    break;
                }
            }
            while (!result.CloseStatus.HasValue && !CancellationToken.IsCancellationRequested);
        }

        private async Task ReceivedMessageFromClientAsync (FluentMessageContract message) {
            if (message.IsReturn)
                await Events.DataReceivedRegister (message.Content);
            else
                await Events.DataReceived (message.Content);
        }

        #region DISPOSE

        public bool Disposed { get; set; } = false;

        private IntPtr nativeResource = Marshal.AllocHGlobal (100);

        ~FluentSocketServer () {
            Dispose (false);
        }

        public void Dispose () {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing) {
            Disposed = true;

            if (disposing) {
                WebSocket?.Dispose ();
            }

            if (nativeResource != IntPtr.Zero) {
                Marshal.FreeHGlobal (nativeResource);
                nativeResource = IntPtr.Zero;
            }
        }

        #endregion
    }
}