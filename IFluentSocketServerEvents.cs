namespace Fluent.Socket
{
    public interface IFluentSocketServerEvents
    {
        void DataReceived(FluentSocketServer fluentSocketServer, object data);
        void PingReceived(FluentSocketServer fluentSocketServer, object data);
        void ClientConnected(FluentSocketServer fluentSocketServer);
        void ClientDisconnected(FluentSocketServer fluentSocketServer);
    }
}