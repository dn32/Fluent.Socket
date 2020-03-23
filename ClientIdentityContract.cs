using System;

namespace Fluent.Socket
{
    [Serializable]
    public class ClientIdentityContract
    {
        public string IpLocal { get; set; }
        public string IpPublico { get; set; }
        public string HostLocal { get; set; }
        public string HostPublico { get; set; }
        public string GatewayLocal { get; set; }
        public object ClientData { get; internal set; }
    }
}