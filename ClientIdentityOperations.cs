using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Fluent.Socket
{
    public static class ClientIdentityOperations
    {
        public static ClientIdentityContract GetClientData()
        {
            var host = Dns.GetHostName();
            var gatewayInterno = GetDefaultGateway();

            string ipLocal = Dns.GetHostAddresses(host)[2].ToString();
            string ipPublico = GetPublicIpAddress();
            var hostName = Dns.GetHostEntry(ipPublico).HostName;

            return new ClientIdentityContract
            {
                HostLocal = Dns.GetHostName(),
                HostPublico = hostName,
                GatewayLocal = gatewayInterno,
                IpLocal = ipLocal,
                IpPublico = ipPublico
            };
        }

        private static string GetPublicIpAddress()
        {
            using var client = new WebClient();
            return client.DownloadString("http://ifconfig.me").Replace("\n", "");
        }

        private static string GetDefaultGateway()
        {
            IPAddress result = null;
            var cards = NetworkInterface.GetAllNetworkInterfaces().ToList();
            if (cards.Any())
            {
                foreach (var card in cards)
                {
                    var props = card.GetIPProperties();
                    if (props == null)
                        continue;

                    var gateways = props.GatewayAddresses;
                    if (!gateways.Any())
                        continue;

                    var gateway =
                        gateways.FirstOrDefault(g => g.Address.AddressFamily.ToString() == "InterNetwork");
                    if (gateway == null)
                        continue;

                    result = gateway.Address;
                    break;
                };
            }

            return result.ToString();
        }
    }
}