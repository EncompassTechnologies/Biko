using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.ComponentModel;

namespace Communications.Net.Proxy
{
    public class Socks5ProxyClient : IProxyClient
    {
        private string _proxyHost;
        private int _proxyPort;
        private string _proxyUserName;
        private string _proxyPassword;
        private SocksAuthentication _proxyAuthMethod;
        private TcpClient _tcpClient;
        private const string PROXY_NAME = "SOCKS5";
        private const int SOCKS5_DEFAULT_PORT = 1080;
        private const byte SOCKS5_VERSION_NUMBER = 5;
        private const byte SOCKS5_RESERVED = 0x00;
        private const byte SOCKS5_AUTH_NUMBER_OF_AUTH_METHODS_SUPPORTED = 2;
        private const byte SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED = 0x00;
        private const byte SOCKS5_AUTH_METHOD_GSSAPI = 0x01;
        private const byte SOCKS5_AUTH_METHOD_USERNAME_PASSWORD = 0x02;
        private const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_BEGIN = 0x03;
        private const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_END = 0x7f;
        private const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_BEGIN = 0x80;
        private const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_END = 0xfe;
        private const byte SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS = 0xff;
        private const byte SOCKS5_CMD_CONNECT = 0x01;
        private const byte SOCKS5_CMD_BIND = 0x02;
        private const byte SOCKS5_CMD_UDP_ASSOCIATE = 0x03;
        private const byte SOCKS5_CMD_REPLY_SUCCEEDED = 0x00;
        private const byte SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE = 0x01;
        private const byte SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET = 0x02;
        private const byte SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE = 0x03;
        private const byte SOCKS5_CMD_REPLY_HOST_UNREACHABLE = 0x04;
        private const byte SOCKS5_CMD_REPLY_CONNECTION_REFUSED = 0x05;
        private const byte SOCKS5_CMD_REPLY_TTL_EXPIRED = 0x06;
        private const byte SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED = 0x07;
        private const byte SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED = 0x08;
        private const byte SOCKS5_ADDRTYPE_IPV4 = 0x01;
        private const byte SOCKS5_ADDRTYPE_DOMAIN_NAME = 0x03;
        private const byte SOCKS5_ADDRTYPE_IPV6 = 0x04;

        private enum SocksAuthentication
        {
            None,
            UsernamePassword
        }

        public Socks5ProxyClient()
        {
        }

        public Socks5ProxyClient(TcpClient tcpClient)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }

            _tcpClient = tcpClient;
        }

        public Socks5ProxyClient(string proxyHost)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            _proxyHost = proxyHost;
            _proxyPort = SOCKS5_DEFAULT_PORT;
        }

        public Socks5ProxyClient(string proxyHost, int proxyPort)
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

        public Socks5ProxyClient(string proxyHost, string proxyUserName, string proxyPassword)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            if (proxyUserName == null)
            {
                throw new ArgumentNullException("proxyUserName");
            }

            if (proxyPassword == null)
            {
                throw new ArgumentNullException("proxyPassword");
            }

            _proxyHost = proxyHost;
            _proxyPort = SOCKS5_DEFAULT_PORT;
            _proxyUserName = proxyUserName;
            _proxyPassword = proxyPassword;
        }

        public Socks5ProxyClient(string proxyHost, int proxyPort, string proxyUserName, string proxyPassword)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            if (proxyPort <= 0 || proxyPort > 65535)
            {
                throw new ArgumentOutOfRangeException("proxyPort", "port must be greater than zero and less than 65535");
            }

            if (proxyUserName == null)
            {
                throw new ArgumentNullException("proxyUserName");
            }

            if (proxyPassword == null)
            {
                throw new ArgumentNullException("proxyPassword");
            }

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
            _proxyUserName = proxyUserName;
            _proxyPassword = proxyPassword;
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

        public string ProxyName
        {
            get
            {
                return PROXY_NAME;
            }
        }

        public string ProxyUserName
        {
            get
            {
                return _proxyUserName;
            }

            set
            {
                _proxyUserName = value;
            }
        }

        public string ProxyPassword
        {
            get
            {
                return _proxyPassword;
            }

            set
            {
                _proxyPassword = value;
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

                DetermineClientAuthMethod();
                NegotiateServerAuthMethod();
                SendCommand(SOCKS5_CMD_CONNECT, destinationHost, destinationPort);
                return _tcpClient;
            }
            catch (Exception ex)
            {
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)), ex);
            }
        }


        private void DetermineClientAuthMethod()
        {
            if (_proxyUserName != null && _proxyPassword != null)
            {
                _proxyAuthMethod = SocksAuthentication.UsernamePassword;
            }
            else
            {
                _proxyAuthMethod = SocksAuthentication.None;
            }
        }

        private void NegotiateServerAuthMethod()
        {
            NetworkStream stream = _tcpClient.GetStream();
            byte[] authRequest = new byte[4];
            authRequest[0] = SOCKS5_VERSION_NUMBER;
            authRequest[1] = SOCKS5_AUTH_NUMBER_OF_AUTH_METHODS_SUPPORTED;
            authRequest[2] = SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED;
            authRequest[3] = SOCKS5_AUTH_METHOD_USERNAME_PASSWORD;
            stream.Write(authRequest, 0, authRequest.Length);
            byte[] response = new byte[2];
            stream.Read(response, 0, response.Length);
            byte acceptedAuthMethod = response[1];

            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS)
            {
                _tcpClient.Close();
                throw new ProxyException("The proxy destination does not accept the supported proxy client authentication methods.");
            }

            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD && _proxyAuthMethod == SocksAuthentication.None)
            {
                _tcpClient.Close();
                throw new ProxyException("The proxy destination requires a username and password for authentication.");
            }

            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD)
            {
                byte[] credentials = new byte[_proxyUserName.Length + _proxyPassword.Length + 3];
                credentials[0] = SOCKS5_VERSION_NUMBER;
                credentials[1] = (byte)_proxyUserName.Length;
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(_proxyUserName), 0, credentials, 2, _proxyUserName.Length);
                credentials[_proxyUserName.Length + 2] = (byte)_proxyPassword.Length;
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(_proxyPassword), 0, credentials, _proxyUserName.Length + 3, _proxyPassword.Length);
            }
        }

        private byte GetDestAddressType(string host)
        {
            IPAddress ipAddr = null;

            bool result = IPAddress.TryParse(host, out ipAddr);

            if (!result)
            {
                return SOCKS5_ADDRTYPE_DOMAIN_NAME;
            }

            switch (ipAddr.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return SOCKS5_ADDRTYPE_IPV4;
                case AddressFamily.InterNetworkV6:
                    return SOCKS5_ADDRTYPE_IPV6;
                default:
                    throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "The host address {0} of type '{1}' is not a supported address type.  The supported types are InterNetwork and InterNetworkV6.", host, Enum.GetName(typeof(AddressFamily), ipAddr.AddressFamily)));
            }

        }

        private byte[] GetDestAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case SOCKS5_ADDRTYPE_IPV4:
                case SOCKS5_ADDRTYPE_IPV6:
                    return IPAddress.Parse(host).GetAddressBytes();
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    byte[] bytes = new byte[host.Length + 1];
                    bytes[0] = Convert.ToByte(host.Length);
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                    return bytes;
                default:
                    return null;
            }
        }

        private byte[] GetDestPortBytes(int value)
        {
            byte[] array = new byte[2];
            array[0] = Convert.ToByte(value / 256);
            array[1] = Convert.ToByte(value % 256);
            return array;
        }

        private void SendCommand(byte command, string destinationHost, int destinationPort)
        {
            NetworkStream stream = _tcpClient.GetStream();

            byte addressType = GetDestAddressType(destinationHost);
            byte[] destAddr = GetDestAddressBytes(addressType, destinationHost);
            byte[] destPort = GetDestPortBytes(destinationPort);
            byte[] request = new byte[4 + destAddr.Length + 2];
            request[0] = SOCKS5_VERSION_NUMBER;
            request[1] = command;
            request[2] = SOCKS5_RESERVED;
            request[3] = addressType;
            destAddr.CopyTo(request, 4);
            destPort.CopyTo(request, 4 + destAddr.Length);
            stream.Write(request, 0, request.Length);
            byte[] response = new byte[255];
            stream.Read(response, 0, response.Length);
            byte replyCode = response[1];

            if (replyCode != SOCKS5_CMD_REPLY_SUCCEEDED)
            {
                HandleProxyCommandError(response, destinationHost, destinationPort);
            }
        }

        private void HandleProxyCommandError(byte[] response, string destinationHost, int destinationPort)
        {
            string proxyErrorText;
            byte replyCode = response[1];
            byte addrType = response[3];
            string addr = "";
            Int16 port = 0;

            switch (addrType)
            {
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    int addrLen = Convert.ToInt32(response[4]);
                    byte[] addrBytes = new byte[addrLen];

                    for (int i = 0; i < addrLen; i++)
                    {
                        addrBytes[i] = response[i + 5];
                    }

                    addr = System.Text.ASCIIEncoding.ASCII.GetString(addrBytes);
                    byte[] portBytesDomain = new byte[2];
                    portBytesDomain[0] = response[6 + addrLen];
                    portBytesDomain[1] = response[5 + addrLen];
                    port = BitConverter.ToInt16(portBytesDomain, 0);
                    break;

                case SOCKS5_ADDRTYPE_IPV4:
                    byte[] ipv4Bytes = new byte[4];

                    for (int i = 0; i < 4; i++)
                    {
                        ipv4Bytes[i] = response[i + 4];
                    }

                    IPAddress ipv4 = new IPAddress(ipv4Bytes);
                    addr = ipv4.ToString();
                    byte[] portBytesIpv4 = new byte[2];
                    portBytesIpv4[0] = response[9];
                    portBytesIpv4[1] = response[8];
                    port = BitConverter.ToInt16(portBytesIpv4, 0);
                    break;

                case SOCKS5_ADDRTYPE_IPV6:
                    byte[] ipv6Bytes = new byte[16];

                    for (int i = 0; i < 16; i++)
                    {
                        ipv6Bytes[i] = response[i + 4];
                    }

                    IPAddress ipv6 = new IPAddress(ipv6Bytes);
                    addr = ipv6.ToString();
                    byte[] portBytesIpv6 = new byte[2];
                    portBytesIpv6[0] = response[21];
                    portBytesIpv6[1] = response[20];
                    port = BitConverter.ToInt16(portBytesIpv6, 0);
                    break;
            }


            switch (replyCode)
            {
                case SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE:
                    proxyErrorText = "a general socks destination failure occurred";
                    break;
                case SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET:
                    proxyErrorText = "the connection is not allowed by proxy destination rule set";
                    break;
                case SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE:
                    proxyErrorText = "the network was unreachable";
                    break;
                case SOCKS5_CMD_REPLY_HOST_UNREACHABLE:
                    proxyErrorText = "the host was unreachable";
                    break;
                case SOCKS5_CMD_REPLY_CONNECTION_REFUSED:
                    proxyErrorText = "the connection was refused by the remote network";
                    break;
                case SOCKS5_CMD_REPLY_TTL_EXPIRED:
                    proxyErrorText = "the time to live (TTL) has expired";
                    break;
                case SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED:
                    proxyErrorText = "the command issued by the proxy client is not supported by the proxy destination";
                    break;
                case SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED:
                    proxyErrorText = "the address type specified is not supported";
                    break;
                default:
                    proxyErrorText = String.Format(CultureInfo.InvariantCulture, "that an unknown reply with the code value '{0}' was received by the destination", replyCode.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            string exceptionMsg = String.Format(CultureInfo.InvariantCulture, "The {0} concerning destination host {1} port number {2}.  The destination reported the host as {3} port {4}.", proxyErrorText, destinationHost, destinationPort, addr, port.ToString(CultureInfo.InvariantCulture));
            throw new ProxyException(exceptionMsg);
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
                throw new InvalidOperationException("The Socks4 object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
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
                CreateConnectionAsyncCompleted(this, new CreateConnectionAsyncCompletedEventArgs(_asyncException, _asyncCancelled, (TcpClient)e.Result));
        }
    }
}
