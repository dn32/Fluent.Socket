using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Fluent.Socket {
    public static class FluentSocketUtil {
        public static async Task SendData<T> (this FluentSocket fluentSocket, T content, CancellationToken cancellationToken) {
            var fluentMessageContract = new FluentMessageContract { Content = content };
            await fluentSocket.SendInternalData (fluentMessageContract, cancellationToken);
        }

        internal static async Task SendInternalData (this FluentSocket fluentSocket, FluentMessageContract fluentMessageContract, CancellationToken cancellationToken) {
            var data = Util.ObjectToByteArray (fluentMessageContract);
            await fluentSocket.WebSocket.SendAsync (new ArraySegment<byte> (data, 0, data.Length), WebSocketMessageType.Binary, true, cancellationToken);
        }

        private static WebSocketOptions WebSocketOptions => new WebSocketOptions () {
            KeepAliveInterval = TimeSpan.FromSeconds (120),
            ReceiveBufferSize = 4 * 1024
        };

        public static IApplicationBuilder UseFluentWebSocket<T> (this IApplicationBuilder app, Func<HttpContext, bool> autentication, string preIdentifier) where T : IFluentSocketServerEvents, new () {
            return app.UseFluentWebSocket<T> (WebSocketOptions, autentication, preIdentifier);
        }

        public static IApplicationBuilder UseFluentWebSocket<T> (this IApplicationBuilder app, WebSocketOptions options, Func<HttpContext, bool> autentication, string preIdentifier) where T : IFluentSocketServerEvents, new () {
            app = app.UseWebSockets (options);

            return app.Use (async (context, next) => {
                if (context.Request.Path == "/ws") {
                    if (autentication (context)) {
                        if (context.WebSockets.IsWebSocketRequest) {
                            var clientSocketId = context.Request.Query["SocketId"].FirstOrDefault ();
                            var events = new T ();
                            var client = new FluentSocketServer (context, events, preIdentifier);
                            events.Initialize (client, clientSocketId, context);
                            Channels.OnlineChannels.TryAdd (clientSocketId, client);
                            try {
                                client.InitAsync (context, (x) => events.ClientConnected ()).Wait ();
                            } catch (Exception ex) {
                                throw ex.InnerException ?? ex;
                            } finally {
                                client.Dispose ();
                                Channels.OnlineChannels.TryRemove (clientSocketId, out _);
                                events.ClientDisconnected ();
                                events.Dispose ();
                            }
                        } else {
                            context.Response.StatusCode = 400;
                        }
                    }
                } else {
                    await next ();
                }
            });
        }

        internal static T Next<T> (this List<T> list) {
            if (list == null) throw new ArgumentNullException (nameof (list));
            if (list.Count == 0) return default;
            var el = list.First ();
            list.RemoveAt (0);
            return el;
        }
    }
}