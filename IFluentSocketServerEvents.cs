using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fluent.Socket
{
    public interface IFluentSocketServerEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketServer fluentSocket, string clientSocketId, HttpContext httpContext);
        void ClientConnected() { }
        void ClientDisconnected() { }
        Task DataReceivedRegister(object content);

        void Dispose();
    }
}