using System;
using System.Linq;
using System.Net;

namespace TrickCore
{
    public static class EndPointExtensions
    {
        public static IPEndPoint Parse(this string endpoint, int defaultport = -1)
        {
            if (string.IsNullOrEmpty(endpoint)
                || endpoint.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                || defaultport > IPEndPoint.MaxPort))
            {
                throw new ArgumentException($"Invalid default port '{defaultport}'");
            }

            string[] values = endpoint.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                if (values.Length == 1)
                    //no port is specified, default
                    port = defaultport;
                else
                    port = GetPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = GetIPFromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = GetPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpoint);
                    port = defaultport;
                }
            }
            else
            {
                throw new FormatException($"Invalid endpoint ip '{endpoint}'");
            }

            if (port == -1)
                throw new ArgumentException($"No port specified: '{endpoint}'");

            return new IPEndPoint(ipaddy, port);
        }

        private static int GetPort(string p)
        {
            if (!int.TryParse(p, out var port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException($"Invalid end point port '{p}'");
            }

            return port;
        }

        private static IPAddress GetIPFromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException($"Host not found: {p}");

            return hosts[0];
        }
    }
}
