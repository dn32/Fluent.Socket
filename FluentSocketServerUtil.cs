using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace Fluent.Socket
{
    public static class FluentSocketServerUtil
    {
        private static WebSocketOptions WebSocketOptions => new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120),
            ReceiveBufferSize = 4 * 1024
        };

        public static IApplicationBuilder UseFluentWebSocket(this IApplicationBuilder app, Func<HttpContext, bool> autentication, IFluentSocketServerEvents events)
        {
            return app.UseFluentWebSocket(WebSocketOptions, autentication, events);
        }

        public static IApplicationBuilder UseFluentWebSocket(this IApplicationBuilder app, WebSocketOptions options, Func<HttpContext, bool> autentication, IFluentSocketServerEvents events)
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
                                var client = new FluentSocketServer(context);
                                try
                                {
                                    client.DataReceived = events.DataReceived;
                                    client.InitAsync(events.ClientConnected).Wait();
                                }
                                catch (Exception) { }
                                client.Dispose();
                                events.ClientDisconnected(client);
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
    }
}