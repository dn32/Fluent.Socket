using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fluent.Socket
{
    public interface IFluentSocketServerEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketServer fluentSocket, string clientSocketId, HttpContext httpContext);
        async Task ClientConnected() => await Task.CompletedTask;
        async Task ClientDisconnected() => await Task.CompletedTask;
        Task DataReceivedRegister(object content);
        void Dispose();
    }
}