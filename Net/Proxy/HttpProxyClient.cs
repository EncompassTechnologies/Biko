using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;
using System.ComponentModel;

namespace Communications.Net.Proxy
{
    public class HttpProxyClient : IProxyClient
    {
        private string _proxyHost;
        private int _proxyPort;
        private HttpResponseCodes _respCode;
        private string _respText;
        private TcpClient _tcpClient;
        private const int HTTP_PROXY_DEFAULT_PORT = 8080;
        private const string HTTP_PROXY_CONNECT_CMD = "CONNECT {0}:{1} HTTP/1.0 \r\nHOST {0}:{1}\r\n\r\n";
        private const int WAIT_FOR_DATA_INTERVAL = 50;
        private const int WAIT_FOR_DATA_TIMEOUT = 15000;
        private const string PROXY_NAME = "HTTP";

        private enum HttpResponseCodes
        {
            None = 0,
            Continue = 100,
            SwitchingProtocols = 101,
            OK = 200,
            Created = 201,
            Accepted = 202,
            NonAuthoritiveInformation = 203,
            NoContent = 204,
            ResetContent = 205,
            PartialContent = 206,
            MultipleChoices = 300,
            MovedPermanetly = 301,
            Found = 302,
            SeeOther = 303,
            NotModified = 304,
            UserProxy = 305,
            TemporaryRedirect = 307,
            BadRequest = 400,
            Unauthorized = 401,
            PaymentRequired = 402,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            NotAcceptable = 406,
            ProxyAuthenticantionRequired = 407,
            RequestTimeout = 408,
            Conflict = 409,
            Gone = 410,
            PreconditionFailed = 411,
            RequestEntityTooLarge = 413,
            RequestURITooLong = 414,
            UnsupportedMediaType = 415,
            RequestedRangeNotSatisfied = 416,
            ExpectationFailed = 417,
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HTTPVersionNotSupported = 505
        }

        public HttpProxyClient()
        {
        }

        public HttpProxyClient(TcpClient tcpClient)
        {
            if (tcpClient == null)
            {
                throw new ArgumentNullException("tcpClient");
            }

            _tcpClient = tcpClient;
        }

        public HttpProxyClient(string proxyHost)
        {
            if (String.IsNullOrEmpty(proxyHost))
            {
                throw new ArgumentNullException("proxyHost");
            }

            _proxyHost = proxyHost;
            _proxyPort = HTTP_PROXY_DEFAULT_PORT;
        }

        public HttpProxyClient(string proxyHost, int proxyPort)
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

        public string ProxyName
        {
            get
            {
                return PROXY_NAME;
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

                SendConnectionCommand(destinationHost, destinationPort);
                return _tcpClient;
            }
            catch (SocketException ex)
            {
                throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)), ex);
            }
        }

        private void SendConnectionCommand(string host, int port)
        {
            NetworkStream stream = _tcpClient.GetStream();
            string connectCmd = String.Format(CultureInfo.InvariantCulture, HTTP_PROXY_CONNECT_CMD, host, port.ToString(CultureInfo.InvariantCulture));
            byte[] request = ASCIIEncoding.ASCII.GetBytes(connectCmd);
            stream.Write(request, 0, request.Length);
            WaitForData(stream);
            byte[] response = new byte[_tcpClient.ReceiveBufferSize];
            StringBuilder sbuilder = new StringBuilder();
            int bytes = 0;
            long total = 0;

            do
            {
                bytes = stream.Read(response, 0, _tcpClient.ReceiveBufferSize);
                total += bytes;
                sbuilder.Append(System.Text.ASCIIEncoding.UTF8.GetString(response, 0, bytes));
            } while (stream.DataAvailable);

            ParseResponse(sbuilder.ToString());

            if (_respCode != HttpResponseCodes.OK)
            {
                HandleProxyCommandError(host, port);
            }
        }

        private void HandleProxyCommandError(string host, int port)
        {
            string msg;

            switch (_respCode)
            {
                case HttpResponseCodes.None:
                    msg = String.Format(CultureInfo.InvariantCulture, "Proxy destination {0} on port {1} failed to return a recognized HTTP response code.  Server response: {2}", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient), _respText);
                    break;
                case HttpResponseCodes.BadGateway:
                    msg = String.Format(CultureInfo.InvariantCulture, "Proxy destination {0} on port {1} responded with a 502 code - Bad Gateway.  If you are connecting to a Microsoft ISA destination please refer to knowledge based article Q283284 for more information.  Server response: {2}", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient), _respText);
                    break;
                default:
                    msg = String.Format(CultureInfo.InvariantCulture, "Proxy destination {0} on port {1} responded with a {2} code - {3}", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient), ((int)_respCode).ToString(CultureInfo.InvariantCulture), _respText);
                    break;
            }

            throw new ProxyException(msg);
        }

        private void WaitForData(NetworkStream stream)
        {
            int sleepTime = 0;

            while (!stream.DataAvailable)
            {
                Thread.Sleep(WAIT_FOR_DATA_INTERVAL);
                sleepTime += WAIT_FOR_DATA_INTERVAL;

                if (sleepTime > WAIT_FOR_DATA_TIMEOUT)
                {
                    throw new ProxyException(String.Format("A timeout while waiting for the proxy server at {0} on port {1} to respond.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)));
                }
            }
        }

        private void ParseResponse(string response)
        {
            string[] data = null;
            data = response.Replace('\n', ' ').Split('\r');
            ParseCodeAndText(data[0]);
        }

        private void ParseCodeAndText(string line)
        {
            int begin = 0;
            int end = 0;
            string val = null;

            if (line.IndexOf("HTTP") == -1)
            {
                throw new ProxyException(String.Format("No HTTP response received from proxy destination.  Server response: {0}.", line));
            }

            begin = line.IndexOf(" ") + 1;
            end = line.IndexOf(" ", begin);
            val = line.Substring(begin, end - begin);
            Int32 code = 0;

            if (!Int32.TryParse(val, out code))
            {
                throw new ProxyException(String.Format("An invalid response code was received from proxy destination.  Server response: {0}.", line));
            }

            _respCode = (HttpResponseCodes)code;
            _respText = line.Substring(end + 1).Trim();
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
                throw new InvalidOperationException("The HttpProxy object is already busy executing another asynchronous operation.  You can only execute one asychronous method at a time.");
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
