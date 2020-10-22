using System;
using System.Threading;

namespace Fluent.Socket
{
    public class RequesInProgress
    {
        public SemaphoreSlim SemaphoreSlim { get; set; }
        public string ReturnJsonContent { get; set; }
        public string RequestId { get; set; }
        public TimeSpan TimeOut { get; set; }
    }
}