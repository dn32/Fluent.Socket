﻿using Microsoft.AspNetCore.Http;
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

        internal Action<object> DataReceived { get; set; } = (object obj) => Console.WriteLine("Data received!");

        #endregion

        public async Task InitAsync()
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            WebSocketReceiveResult result = null;

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
                    DataReceived(obj?.Content);
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