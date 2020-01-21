namespace Fluent.Socket
{
    public interface IFluentSocketClientEvents
    {
        void DataReceived(FluentSocketClient fluentSocketClient, object data);
        void Connecting(FluentSocketClient fluentSocketClient);
        void Connected(FluentSocketClient fluentSocketClient);
        void LossOfConnection(FluentSocketClient fluentSocketClient, string message);
        void PingOk(FluentSocketClient fluentSocketClient);
    }
}