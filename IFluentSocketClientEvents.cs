namespace Fluent.Socket
{
    public interface IFluentSocketClientEvents : IFluentSocketEvents
    {
        void Initialize(FluentSocketClient fluentSocket, string clientSocketId);
        void Connecting() { }
        void Connected();
        void LossOfConnection(string errorMessage);
        object GetClientData();
    }
}