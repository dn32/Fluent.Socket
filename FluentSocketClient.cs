﻿using Fluent.Socket.Contracts;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public class FluentSocketClient : FluentSocket
    {
        #region PROPERTIES

        public Uri Uri { get; set; }
        public string Url { get; set; }
        private new IFluentSocketClientEvents Events => base.Events as IFluentSocketClientEvents;
        public int ReconnectInterval { get; set; } = 2000;
        public int PingInterval { get; set; } = 5000;

        #endregion

        private FluentSocketClient(IFluentSocketClientEvents events, string preIdentifier, string url) : base(preIdentifier)
        {
            base.Events = events;
            base.WebSocket = new ClientWebSocket();
            Url = url;
            Url += Url.Contains("?") ? "&" : "?";
            Url += $"SocketId={WebUtility.UrlEncode(SocketId)}";
            Uri = new Uri(Url);
        }

        public async Task CloseConnectionAsync()
        {
            try
            {
                Finished = true;
                await WebSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken());
            }
            catch { }

            WebSocket?.Dispose();
        }

        public static void Initialize(string url, IFluentSocketClientEvents events, string preIdentifier, CancellationToken cancellationToken, bool useJson = false)
        {
            var instance = new FluentSocketClient(events, preIdentifier, url)
            {
                CancellationToken = cancellationToken,
                UseJson = useJson
            };

            events.Initialize(instance, instance.SocketId);

            _ = instance.ConectSocket();
            _ = instance.StartReceivingData();
        }

        private async Task ConectSocket()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (base.WebSocket.State == WebSocketState.Open)
                    {
                        await Task.Delay(PingInterval);
                        continue;
                    }
                    else
                    {
                        if (Finished) return;

                        Channels.MyServer = null;
                        base.WebSocket?.Dispose();
                        base.WebSocket = new ClientWebSocket();
                    }

                    await Events.Connecting();
                    await ((ClientWebSocket)base.WebSocket).ConnectAsync(Uri, CancellationToken);

                    var clientData = Events.GetClientData();
                    await this.SendInternalData(new FluentRegisterMessageContract(clientData), CancellationToken);
                    await Events.Connected();
                }
                catch (WebSocketException ex)
                {
                    Channels.MyServer = null;
                    await Events.LossOfConnection(ex.Message);
                    await Task.Delay(ReconnectInterval);
                    base.WebSocket.Dispose();
                }
            }
        }

        private async Task StartReceivingData()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                if (Finished) return;
                try
                {
                    var message = await ReceiveFromServerData();
                    if (message == null)
                    {
                        await Task.Delay(200);
                    }
                    else if (message is FluentDefaultMessageContract default_)
                    {
                        await Events.DataReceived(message.Content);
                    }
                    else if (message is FluentWaitMessageContract waitMesssage)
                    {
                        var returnContent = await Events.DataReceivedAndWait(waitMesssage.Content);
                        var returnContentObj = JsonConvert.SerializeObject(returnContent);
                        var returnContract = new FluentReturnMessageContract(returnContentObj) { OriginalRequestId = waitMesssage.RequestId };
                        await this.SendInternalData(returnContract, CancellationToken);
                    }
                    else if (message is FluentReturnMessageContract returnMessage)
                    {
                        await FluentSocketUtil.ReturnReceivedFromClient(returnMessage);
                    }
                }
                catch (Exception ex)
                {
                    await Task.Delay(ReconnectInterval);
                    continue;
                }
            }
        }

        private async Task<FluentMessageContractBase> ReceiveFromServerData()
        {
            if (WebSocket == null || WebSocket.State != WebSocketState.Open) return null;

            using var ms = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult result;
            do
            {
                result = await WebSocket.ReceiveAsync(buffer, CancellationToken);
                await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            return Util.DeserializeFromStream<FluentMessageContractBase>(ms);
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