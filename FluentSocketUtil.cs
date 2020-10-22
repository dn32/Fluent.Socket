using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluent.Socket.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Fluent.Socket
{
    public static class FluentSocketUtil
    {
        private static readonly TimeSpan DefaultTimeOutRequest = TimeSpan.FromSeconds(20);
        private static readonly ConcurrentDictionary<string, RequesInProgress> Returs = new ConcurrentDictionary<string, RequesInProgress>();
        public static bool MostrarLogsDeDebug { get; set; } = false;

        public static async Task SendData(this FluentSocket fluentSocket, object content, CancellationToken cancellationToken)
        {
            var fluentMessageContract = new FluentDefaultMessageContract(content);
            await fluentSocket.SendInternalData(fluentMessageContract, cancellationToken);
        }

        public static async Task<FluentReturnMessage> SendDataAndWait<TO>(this FluentSocket fluentSocket, object content, CancellationToken cancellationToken, TimeSpan timeOut = default) where TO : class
        {
            var stopwatch = new Stopwatch(); stopwatch.Start();
            var contractBase = content as FluentMessageContractBase;
            var fluentMessageContract = contractBase ?? new FluentWaitMessageContract(fluentSocket.UseJson ? JsonConvert.SerializeObject(content) : content);

            var request = new RequesInProgress
            {
                SemaphoreSlim = new SemaphoreSlim(0),
                RequestId = fluentMessageContract.RequestId,
                TimeOut = timeOut == default ? DefaultTimeOutRequest : timeOut
            };

            Returs.TryAdd(fluentMessageContract.RequestId, request);

            await fluentSocket.SendInternalData(fluentMessageContract, cancellationToken);
     
            LogDeDebug($"SendDataAndWait({Thread.CurrentThread.ManagedThreadId}) {stopwatch.ElapsedMilliseconds}");
            var sucess = await WaitReturn(request);
            LogDeDebug($"SendDataAndWaitRetorno({Thread.CurrentThread.ManagedThreadId}) {stopwatch.ElapsedMilliseconds}");

            Returs.TryRemove(fluentMessageContract.RequestId, out _);

            return new FluentReturnMessage
            {
                Sucess = sucess,
                Content = request.ReturnJsonContent == null ? null : JsonConvert.DeserializeObject<TO>(request.ReturnJsonContent)
            };
        }

        internal static Task ReturnReceivedFromClient(FluentReturnMessageContract returnMessage)
        {
            if (Returs.TryGetValue(returnMessage.OriginalRequestId, out var ret))
            {
                ret.ReturnJsonContent = returnMessage.Content.ToString();
                ret.SemaphoreSlim.Release();
            }

            return Task.CompletedTask;
        }

        private static async Task<bool> WaitReturn(RequesInProgress requesInProgress)
        {
            return await requesInProgress.SemaphoreSlim.WaitAsync(requesInProgress.TimeOut);
        }

        internal static async Task SendInternalData(this FluentSocket fluentSocket, FluentMessageContractBase fluentMessageContract, CancellationToken cancellationToken)
        {
            if (fluentSocket.UseJson)
            {
                var json = JsonConvert.SerializeObject(fluentMessageContract);
                var data = Encoding.ASCII.GetBytes(json);
                await fluentSocket.WebSocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true, cancellationToken);
            }
            else
            {
                var data = Util.ObjectToByteArray(fluentMessageContract);
                await fluentSocket.WebSocket.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Binary, true, cancellationToken);
            }
        }

        private static WebSocketOptions WebSocketOptions => new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120),
            ReceiveBufferSize = 4 * 1024
        };

        public static IApplicationBuilder UseFluentWebSocket<T>(this IApplicationBuilder app, Func<HttpContext, bool> autentication, string preIdentifier) where T : IFluentSocketServerEvents, new()
        {
            return app.UseFluentWebSocket<T>(WebSocketOptions, autentication, preIdentifier);
        }

        public static IApplicationBuilder UseFluentWebSocket<T>(this IApplicationBuilder app, Func<HttpContext, bool> autentication, string preIdentifier, string path, bool useJson) where T : IFluentSocketServerEvents, new()
        {
            return app.UseFluentWebSocket<T>(WebSocketOptions, autentication, preIdentifier, path, useJson);
        }

        public static IApplicationBuilder UseFluentWebSocket<T>(this IApplicationBuilder app, WebSocketOptions options, Func<HttpContext, bool> autentication, string preIdentifier, string path = "ws", bool useJson = false) where T : IFluentSocketServerEvents, new()
        {
            app = app.UseWebSockets(options);

            return app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/" + path)
                {
                    if (autentication(context))
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var clientSocketId = context.Request.Query["SocketId"].FirstOrDefault();
                            var events = new T();
                            var client = new FluentSocketServer(context, events, preIdentifier, useJson);
                            events.Initialize(client, clientSocketId, context);
                            Channels.OnlineChannels.TryAdd(clientSocketId, client);
                            try
                            {
                                await client.InitAsync(context, async (x) => await events.ClientConnected());
                            }
                            catch (Exception ex)
                            {
                                throw ex.InnerException ?? ex;
                            }
                            finally
                            {
                                client.Dispose();
                                Channels.OnlineChannels.TryRemove(clientSocketId, out _);
                                await events.ClientDisconnected();
                                events.Dispose();
                            }
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

        public static void LogDeDebug(string mensagem)
        {
            if (MostrarLogsDeDebug)
            {
                Console.WriteLine(mensagem);
            }
        }
    }
}