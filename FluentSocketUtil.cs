using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public static class FluentSocketUtil
    {
        public static async Task SendData(this FluentSocket fluentSocket, FluentMessageContract fluentMessageContract, CancellationToken cancellationToken)
        {
            fluentMessageContract.Sender.Add(fluentSocket.SocketId);
            var data = Util.ObjectToByteArray(fluentMessageContract);
            await fluentSocket.WebSocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Binary, true, cancellationToken);
        }
     
        private static WebSocketOptions WebSocketOptions => new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120),
            ReceiveBufferSize = 4 * 1024
        };

        public static IApplicationBuilder UseFluentWebSocket(this IApplicationBuilder app, Func<HttpContext, bool> autentication, IFluentSocketServerEvents events, string preIdentifier)
        {
            return app.UseFluentWebSocket(WebSocketOptions, autentication, events, preIdentifier);
        }

        public static IApplicationBuilder UseFluentWebSocket(this IApplicationBuilder app, WebSocketOptions options, Func<HttpContext, bool> autentication, IFluentSocketServerEvents events, string preIdentifier)
        {
            app = app.UseWebSockets(options);

            return app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (autentication(context))
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var clientSocketId = context.Request.Query["SocketId"].First();
                            
                            var client = new FluentSocketServer(context, events, preIdentifier);
                            Channels.OnlineChannels.TryAdd(clientSocketId, client);
                            try
                            {
                                client.InitAsync((x) => events.ClientConnectedAsync(x, clientSocketId).Wait()).Wait();
                            }
                            catch (Exception) { }
                            client.Dispose();
                            Channels.OnlineChannels.TryRemove(clientSocketId, out _);
                            await events.ClientDisconnectedAsync(client, clientSocketId);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        internal static T Next<T>(this List<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return default;
            var el = list.First();
            list.RemoveAt(0);
            return el;
        }
    }
}