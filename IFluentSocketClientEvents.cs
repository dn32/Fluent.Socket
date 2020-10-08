using System.Threading.Tasks;

namespace Fluent.Socket
{
    public interface IFluentSocketClientEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketClient fluentSocket, string clientSocketId);
        async Task Connecting() => await Task.CompletedTask;
        async Task Connected() => await Task.CompletedTask;
        void LossOfConnection(string errorMessage);
        object GetClientData();
    }
}