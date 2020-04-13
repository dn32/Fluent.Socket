
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public interface IFluentSocketEvents
    {
        Task DataReceived(FluentMessageContract fluentMessageContract);
    }

    public interface IFluentSocketClientEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketClient fluentSocket, string clientSocketId);
        void Connecting() { }
        void Connected();
        void LossOfConnection(string errorMessage);
        object GetClientData();
    }

    public interface IFluentSocketServerEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketServer fluentSocket, string clientSocketId, HttpContext httpContext);
        void ClientConnected() { }
        void ClientDisconnected() { }
        void Dispose();
    }
}