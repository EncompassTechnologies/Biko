using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace Communications.Net.Proxy
{
    public class Socks4ProxyClient : IProxyClient
    {
        private const int WAIT_FOR_DATA_INTERVAL = 50;
        private const int WAIT_FOR_DATA_TIMEOUT = 15000;
        private const string PROXY_NAME = "SOCKS4";
        private TcpClient _tcpClient;
        private string _proxyHost;
        private int _proxyPort;
        private string _proxyUserId;

        internal const int SOCKS_PROXY_DEFAULT_PORT = 1080;
        internal const byte SOCKS4_VERSION_NUMBER = 4;
        internal const byte SOCKS4_CMD_CONNECT = 0x01;
        internal const byte SOCKS4_CMD_BIND = 0x02;
        internal const byte SOCKS4_CMD_REPLY_REQUEST_GRANTED = 90;
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED = 91;
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD = 92;
        internal const byte SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD = 93;

        public Socks4ProxyClient()
        {
        }

        public Socks4ProxyClient(TcpClient tcpClient)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }

            _tcpClient = tcpClient;
        }

        public Socks4ProxyClient(string proxyHost, string proxyUserId)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            if (proxyUserId == null)
            {
                throw new ArgumentNullException("proxyUserId");
            }

            _proxyHost = proxyHost;
            _proxyPort = SOCKS_PROXY_DEFAULT_PORT;
            _proxyUserId = proxyUserId;
        }

        public Socks4ProxyClient(string proxyHost, int proxyPort, string proxyUserId)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            if (proxyPort <= 0 || proxyPort > 65535)
            {
                throw new ArgumentOutOfRangeException("proxyPort", "port must be greater than zero and less than 65535");
            }

            if (proxyUserId == null)
            {
                throw new ArgumentNullException("proxyUserId");
            }

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
            _proxyUserId = proxyUserId;
        }

        public Socks4ProxyClient(string proxyHost)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            _proxyHost = proxyHost;
            _proxyPort = SOCKS_PROXY_DEFAULT_PORT;
        }

        public Socks4ProxyClient(string proxyHost, int proxyPort)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            if (proxyPort <= 0 || proxyPort > 65535)
            {
                throw new ArgumentOutOfRangeException("proxyPort", "port must be greater than zero and less than 65535");
            }

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
        }

        public string ProxyHost
        {
            get
            {
                return _proxyHost;
            }

            set
            {
                _proxyHost = value;
            }
        }

        public int ProxyPort
        {
            get
            {
                return _proxyPort;
            }

            set
            {
                _proxyPort = value;
            }
        }

        virtual public string ProxyName
        {
            get
            {
                return PROXY_NAME;
            }
        }

        public string ProxyUserId
        {
            get
            {
                return _proxyUserId;
            }

            set
            {
                _proxyUserId = value;
            }
        }

        public TcpClient TcpClient
        {
            get
            {
                return _tcpClient;
            }

            set
            {
                _tcpClient = value;
            }
        }

        public TcpClient CreateConnection(string destinationHost, int destinationPort)
        {
            if (String.IsNullOrEmpty(destinationHost))
            {
                throw new ArgumentNullException("destinationHost");
            }

            if (destinationPort <= 0 || destinationPort > 65535)
            {
                throw new ArgumentOutOfRangeException("destinationPort", "port must be greater than zero and less than 65535");
            }

            try
            {
                if (_tcpClient == null)
                {
                    if (String.IsNullOrEmpty(_proxyHost))
                    {
                        throw new ProxyException("ProxyHost property must contain a value.");
                    }

                    if (_proxyPort <= 0 || _proxyPort > 65535)
                    {
                        throw new ProxyException("ProxyPort value must be greater than zero and less than 65535");
                    }

                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(_proxyHost, _proxyPort);
                }

                SendCommand(_tcpClient.GetStream(), SOCKS4_CMD_CONNECT, destinationHost, destinationPort, _proxyUserId);
                return _tcpClient;
            }
            catch (Exception ex)
            {
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)), ex);
            }
        }

        internal virtual void SendCommand(NetworkStream proxy, byte command, string destinationHost, int destinationPort, string userId)
        {
            if (userId == null)
            {
                userId = "";
            }

            byte[] destIp = GetIPAddressBytes(destinationHost);
            byte[] destPort = GetDestinationPortBytes(destinationPort);
            byte[] userIdBytes = ASCIIEncoding.ASCII.GetBytes(userId);
            byte[] request = new byte[9 + userIdBytes.Length];

            request[0] = SOCKS4_VERSION_NUMBER;
            request[1] = command;
            destPort.CopyTo(request, 2);
            destIp.CopyTo(request, 4);
            userIdBytes.CopyTo(request, 8);
            request[8 + userIdBytes.Length] = 0x00;
            proxy.Write(request, 0, request.Length);
            WaitForData(proxy);
            byte[] response = new byte[8];
            proxy.Read(response, 0, 8);

            if (response[1] != SOCKS4_CMD_REPLY_REQUEST_GRANTED)
            {
                HandleProxyCommandError(response, destinationHost, destinationPort);
            }
        }

        internal byte[] GetIPAddressBytes(string destinationHost)
        {
            IPAddress ipAddr = null;

            if (!IPAddress.TryParse(destinationHost, out ipAddr))
            {
                try
                {
                    ipAddr = Dns.GetHostEntry(destinationHost).AddressList[0];
                }
                catch (Exception ex)
                {
                    throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "A error occurred while attempting to DNS resolve the host name {0}.", destinationHost), ex);
                }
            }

            return ipAddr.GetAddressBytes();
        }

        internal byte[] GetDestinationPortBytes(int value)
        {
            byte[] array = new byte[2];
            array[0] = Convert.ToByte(value / 256);
            array[1] = Convert.ToByte(value % 256);
            return array;
        }

        internal void HandleProxyCommandError(byte[] response, string destinationHost, int destinationPort)
        {

            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            byte replyCode = response[1];
            byte[] ipBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                ipBytes[i] = response[i + 4];
            }

            IPAddress ipAddr = new IPAddress(ipBytes);
            byte[] portBytes = new byte[2];
            portBytes[0] = response[3];
            portBytes[1] = response[2];
            Int16 port = BitConverter.ToInt16(portBytes, 0);
            string proxyErrorText;

            switch (replyCode)
            {
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_OR_FAILED:
                    proxyErrorText = "connection request was rejected or failed";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_CANNOT_CONNECT_TO_IDENTD:
                    proxyErrorText = "connection request was rejected because SOCKS destination cannot connect to identified on the client";
                    break;
                case SOCKS4_CMD_REPLY_REQUEST_REJECTED_DIFFERENT_IDENTD:
                    proxyErrorText = "connection request rejected because the client program and identified report different user-ids";
                    break;
                default:
                    proxyErrorText = String.Format(CultureInfo.InvariantCulture, "proxy client received an unknown reply with the code value '{0}' from the proxy destination", replyCode.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            string exceptionMsg = String.Format(CultureInfo.InvariantCulture, "The {0} concerning destination host {1} port number {2}.  The destination reported the host as {3} port {4}.", proxyErrorText, destinationHost, destinationPort, ipAddr.ToString(), port.ToString(CultureInfo.InvariantCulture));
            throw new ProxyException(exceptionMsg);
        }

        internal void WaitForData(NetworkStream stream)
        {
            int sleepTime = 0;

            while (!stream.DataAvailable)
            {
                Thread.Sleep(WAIT_FOR_DATA_INTERVAL);
                sleepTime += WAIT_FOR_DATA_INTERVAL;

                if (sleepTime > WAIT_FOR_DATA_TIMEOUT)
                {
                    throw new ProxyException("A timeout while waiting for the proxy destination to respond.");
                }
            }
        }

        private BackgroundWorker _asyncWorker;
        private Exception _asyncException;
        bool _asyncCancelled;

        public bool IsBusy
        {
            get
            {
                return _asyncWorker == null ? false : _asyncWorker.IsBusy;
            }
        }

        public bool IsAsyncCancelled
        {
            get
            {
                return _asyncCancelled;
            }
        }

        public void CancelAsync()
        {
            if (_asyncWorker != null && !_asyncWorker.CancellationPending && _asyncWorker.IsBusy)
            {
                _asyncCancelled = true;
                _asyncWorker.CancelAsync();
            }
        }

        private void CreateAsyncWorker()
        {
            if (_asyncWorker != null)
            {
                _asyncWorker.Dispose();
            }

            _asyncException = null;
            _asyncWorker = null;
            _asyncCancelled = false;
            _asyncWorker = new BackgroundWorker();
        }

        public event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;

        public void CreateConnectionAsync(string destinationHost, int destinationPort)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The Socks4/4a object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(CreateConnectionAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CreateConnectionAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = destinationHost;
            args[1] = destinationPort;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void CreateConnectionAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                e.Result = CreateConnection((string)args[0], (int)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void CreateConnectionAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (CreateConnectionAsyncCompleted != null)
            {
                CreateConnectionAsyncCompleted(this, new CreateConnectionAsyncCompletedEventArgs(_asyncException, _asyncCancelled, (TcpClient)e.Result));
            }
        }
    }
}
