
using System.Threading.Tasks;

namespace Fluent.Socket
{
    public interface IFluentSocketEvents
    {
        Task DataReceived(FluentSocket fluentSocket, FluentMessageContract fluentMessageContract);
    }

    public interface IFluentSocketClientEvents : IFluentSocketEvents
    {
        void Connecting(FluentSocketClient fluentSocket);
        Task ConnectedAsync(FluentSocketClient fluentSocket);
        void LossOfConnection(FluentSocketClient fluentSocket, string message);
        void PingOk(FluentSocketClient fluentSocket);
        Task InitialInformationReceived(FluentSocketClient fluentSocket, FluentMessageContract fluentMessageContract);
    }

    public interface IFluentSocketServerEvents : IFluentSocketEvents
    {
        void PingReceived(FluentSocketServer fluentSocket, FluentMessageContract fluentMessageContract);
        Task ClientConnectedAsync(FluentSocketServer fluentSocket);
        Task ClientDisconnectedAsync(FluentSocketServer fluentSocket);
        object GetInitialInformation(FluentSocketServer fluentSocketServer);
    }
}