using System.Threading.Tasks;

namespace Fluent.Socket
{
    public interface IFluentSocketEvents
    {
        Task DataReceived(object content);
        Task<object> DataReceivedAndWait(object content);
    }
}