using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Communications.Net.Proxy
{
    public class Socks4aProxyClient : Socks4ProxyClient
    {
        private const string PROXY_NAME = "SOCKS4a";

        public Socks4aProxyClient()
            : base()
        {
        }

        public Socks4aProxyClient(TcpClient tcpClient)
            : base(tcpClient)
        {
        }

        public Socks4aProxyClient(string proxyHost, string proxyUserId)
            : base(proxyHost, proxyUserId)
        {
        }

        public Socks4aProxyClient(string proxyHost, int proxyPort, string proxyUserId)
            : base(proxyHost, proxyPort, proxyUserId)
        {
        }

        public Socks4aProxyClient(string proxyHost)
            : base(proxyHost)
        {
        }

        public Socks4aProxyClient(string proxyHost, int proxyPort)
            : base(proxyHost, proxyPort)
        {
        }

        public override string ProxyName
        {
            get
            {
                return PROXY_NAME;
            }
        }

        internal override void SendCommand(NetworkStream proxy, byte command, string destinationHost, int destinationPort, string userId)
        {
            if (userId == null)
            {
                userId = "";
            }

            byte[] destIp = { 0, 0, 0, 1 };
            byte[] destPort = GetDestinationPortBytes(destinationPort);
            byte[] userIdBytes = ASCIIEncoding.ASCII.GetBytes(userId);
            byte[] hostBytes = ASCIIEncoding.ASCII.GetBytes(destinationHost);
            byte[] request = new byte[10 + userIdBytes.Length + hostBytes.Length];

            request[0] = SOCKS4_VERSION_NUMBER;
            request[1] = command;
            destPort.CopyTo(request, 2);
            destIp.CopyTo(request, 4);
            userIdBytes.CopyTo(request, 8);
            request[8 + userIdBytes.Length] = 0x00;
            hostBytes.CopyTo(request, 9 + userIdBytes.Length);
            request[9 + userIdBytes.Length + hostBytes.Length] = 0x00;
            proxy.Write(request, 0, request.Length);
            base.WaitForData(proxy);
            byte[] response = new byte[8];
            proxy.Read(response, 0, 8);

            if (response[1] != SOCKS4_CMD_REPLY_REQUEST_GRANTED)
            {
                HandleProxyCommandError(response, destinationHost, destinationPort);
            }
        }
    }
}
