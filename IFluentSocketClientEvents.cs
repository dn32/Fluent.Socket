using System.Threading.Tasks;

namespace Fluent.Socket
{
    public interface IFluentSocketClientEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketClient fluentSocket, string clientSocketId);
        async Task ConnectingAsync() => await Task.CompletedTask;
        async Task SocketOpen() => await Task.CompletedTask;
        async Task ConnectedAsync(FluentReturnMessage autenticationResult) => await Task.CompletedTask;
        async Task LossOfConnectionAsync(string errorMessage) => await Task.CompletedTask;
        object GetClientData();
    }
}