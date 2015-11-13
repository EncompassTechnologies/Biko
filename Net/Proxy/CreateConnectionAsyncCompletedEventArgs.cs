using System;
using System.Net.Sockets;
using System.ComponentModel;

namespace Communications.Net.Proxy
{
    public class CreateConnectionAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        private TcpClient _proxyConnection;

        public CreateConnectionAsyncCompletedEventArgs(Exception error, bool cancelled, TcpClient proxyConnection)
            : base(error, cancelled, null)
        {
            _proxyConnection = proxyConnection;
        }

        public TcpClient ProxyConnection
        {
            get
            {
                return _proxyConnection;
            }
        }
    }
}
