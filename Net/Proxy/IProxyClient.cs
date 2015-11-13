using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Communications.Net.Proxy
{
    public interface IProxyClient
    {
        event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;

        string ProxyHost
        {
            get;
            set;
        }

        int ProxyPort
        {
            get;
            set;
        }

        string ProxyName
        {
            get;
        }

        TcpClient TcpClient
        {
            get;
            set;
        }

        TcpClient CreateConnection(string destinationHost, int destinationPort);

        void CreateConnectionAsync(string destinationHost, int destinationPort);
    }
}
