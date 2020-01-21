namespace Fluent.Socket
{
    public interface IFluentSocketServerEvents
    {
        void DataReceived(object data);
        void ClientConnected(FluentSocketServer fluentSocketServer);
        void ClientDisconnected(FluentSocketServer fluentSocketServer);
    }
}