using System;
using System.Collections.Concurrent;

namespace Fluent.Socket
{
    public class Channels
    {
        public static ConcurrentDictionary<string, FluentSocket> OnlineChannels { get; set; } = new ConcurrentDictionary<string, FluentSocket>();
        public static Tuple<string, FluentSocket> MyServer { get; set; }
    }
}