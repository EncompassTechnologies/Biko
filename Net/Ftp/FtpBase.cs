using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.IO.Compression;
using System.Diagnostics;
using Communications.Net.Proxy;

namespace Communications.Net.Ftp
{
    public enum TransferType : int
    {
        None,
        Ascii,
        Binary
    }

    public enum FileAction : int
    {
        None,
        Create,
        CreateNew,
        CreateOrAppend,
        Resume,
        ResumeOrCreate
    }

    public enum TransferMode : int
    {
        Active,
        Passive
    }

    internal enum TransferDirection : int
    {
        ToClient,
        ToServer
    }

    public enum HashingFunction : int
    {
        None,
        Crc32,
        Md5,
        Sha1
    }

    public enum FtpResponseCode : int
    {
        None = 0,
        CommandOkay = 200,
        SyntaxErrorCommandUnrecognized = 500,
        SyntaxErrorInParametersOrArguments = 501,
        CommandNotImplementedSuperfluousAtThisSite = 202,
        CommandNotImplemented = 502,
        BadSequenceOfCommands = 503,
        CommandNotImplementedForThatParameter = 504,
        RestartMarkerReply = 110,
        SystemStatusOrHelpReply = 211,
        DirectoryStatus = 212,
        FileStatus = 213,
        HelpMessage = 214,
        NameSystemType = 215,
        ServiceReadyInxxxMinutes = 120,
        ServiceReadyForNewUser = 220,
        ServiceClosingControlConnection = 221,
        ServiceNotAvailableClosingControlConnection = 421,
        DataConnectionAlreadyOpenSoTransferStarting = 125,
        DataConnectionOpenSoNoTransferInProgress = 225,
        CannotOpenDataConnection = 425,
        ClosingDataConnection = 226,
        ConnectionClosedSoTransferAborted = 426,
        EnteringPassiveMode = 227,
        UserLoggedIn = 230,
        NotLoggedIn = 530,
        UserNameOkayButNeedPassword = 331,
        NeedAccountForLogin = 332,
        NeedAccountForStoringFiles = 532,
        FileStatusOkaySoAboutToOpenDataConnection = 150,
        RequestedFileActionOkayAndCompleted = 250,
        PathNameCreated = 257,
        RequestedFileActionPendingFurtherInformation = 350,
        RequestedFileActionNotTaken = 450,
        RequestedActionNotTakenFileUnavailable = 550,
        RequestedActionAbortedDueToLocalErrorInProcessing = 451,
        RequestedActionAbortedPageTypeUnknown = 551,
        RequestedActionNotTakenInsufficientStorage = 452,
        RequestedFileActionAbortedExceededStorageAllocation = 552,
        RequestedActionNotTakenFileNameNotAllowed = 553,
        AuthenticationCommandOkay = 234,
        ServiceIsUnavailable = 431
    }

    public enum FtpSecurityProtocol : int
    {
        None,
        Tls1Explicit,
        Tls1OrSsl3Explicit,
        Ssl3Explicit,
        Ssl2Explicit,
        Tls1Implicit,
        Tls1OrSsl3Implicit,
        Ssl3Implicit,
        Ssl2Implicit
    }

    public abstract class FtpBase : IDisposable
    {
        internal FtpBase(int port, FtpSecurityProtocol securityProtocol)
        {
            _port = port;
            _securityProtocol = securityProtocol;
        }

        internal FtpBase(string host, int port, FtpSecurityProtocol securityProtocol)
        {
            _host = host;
            _port = port;
            _securityProtocol = securityProtocol;
        }

        private TcpClient _commandConn;
        private Stream _commandStream;
        private TcpClient _dataConn;
        private int _port;
        private string _host;
        private TransferMode _dataTransferMode = TransferMode.Passive;
        private FtpResponseQueue _responseQueue = new FtpResponseQueue();
        private FtpResponse _response = new FtpResponse();
        private FtpResponseCollection _responseList = new FtpResponseCollection();
        private Thread _responseMonitor;

        static object _reponseMonitorLock = new object();
        
        private IProxyClient _proxy;
        private int _maxUploadSpeed;
        private int _maxDownloadSpeed;
        private int _tcpBufferSize = TCP_BUFFER_SIZE;
        private int _tcpTimeout = TCP_TIMEOUT;
        private int _transferTimeout = TRANSFER_TIMEOUT;
        private int _commandTimeout = COMMAND_TIMEOUT;
        private TcpListener _activeListener;
        private int _activePort;
        private int _activePortRangeMin = 50000;
        private int _activePortRangeMax = 50080;
        private FtpSecurityProtocol _securityProtocol = FtpSecurityProtocol.None;
        private X509Certificate2 _serverCertificate;
        private X509CertificateCollection _clientCertificates = new X509CertificateCollection();
        private bool _isCompressionEnabled;
        private HashingFunction _hashAlgorithm;
        private Encoding _encoding = Encoding.UTF8;
        private ManualResetEvent _activeSignal = new ManualResetEvent(false);
        private BackgroundWorker _asyncWorker;
        private bool _asyncCanceled;
        private const int TCP_BUFFER_SIZE = 8192;
        private const int TCP_TIMEOUT = 600000;
        private const int WAIT_FOR_DATA_INTERVAL = 50;
        private const int WAIT_FOR_COMMAND_RESPONSE_INTERVAL = 50;
        private const int TRANSFER_TIMEOUT = 600000;
        private const int COMMAND_TIMEOUT = 600000;

        public event EventHandler<FtpResponseEventArgs> ServerResponse;
        public event EventHandler<FtpRequestEventArgs> ClientRequest;
        public event EventHandler<TransferProgressEventArgs> TransferProgress;
        public event EventHandler<TransferCompleteEventArgs> TransferComplete;
        public event EventHandler<ValidateServerCertificateEventArgs> ValidateServerCertificate;
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        public void CancelAsync()
        {
            if (_asyncWorker != null && !_asyncWorker.CancellationPending && _asyncWorker.IsBusy)
            {
                _asyncCanceled = true;
                _asyncWorker.CancelAsync();
            }
        }

        public string GetChecksum(HashingFunction hash, string path)
        {
            return GetChecksum(hash, path, 0, 0);
        }

        public string GetChecksum(HashingFunction hash, string path, long startPosition, long endPosition)
        {
            if (hash == HashingFunction.None)
            {
                throw new ArgumentOutOfRangeException("hash", "must contain a value other than 'Unknown'");
            }

            if (startPosition < 0)
            {
                throw new ArgumentOutOfRangeException("startPosition", "must contain a value greater than or equal to 0");
            }

            if (endPosition < 0)
            {
                throw new ArgumentOutOfRangeException("startPosition", "must contain a value greater than or equal to 0");
            }

            if (startPosition > endPosition)
            {
                throw new ArgumentOutOfRangeException("startPosition", "must contain a value less than or equal to endPosition");
            }

            FtpCmd command = FtpCmd.Unknown;

            switch (hash)
            {
                case HashingFunction.Crc32:
                    command = FtpCmd.Xcrc;
                    break;
                case HashingFunction.Md5:
                    command = FtpCmd.Xmd5;
                    break;
                case HashingFunction.Sha1:
                    command = FtpCmd.Xsha1;
                    break;
            }

            if (startPosition > 0)
            {
                SendRequest(new FtpRequest(_encoding, command, path, startPosition.ToString(), endPosition.ToString()));
            }
            else
            {
                SendRequest(new FtpRequest(_encoding, command, path));
            }

            return _response.Text;
        }

        public string ComputeChecksum(HashingFunction hash, string localPath)
        {
            if (!File.Exists(localPath))
            {
                throw new ArgumentException("file does not exist.", "localPath");
            }

            using (FileStream fileStream = File.OpenRead(localPath))
            {
                return ComputeChecksum(hash, fileStream);
            }
        }

        public string ComputeChecksum(HashingFunction hash, Stream inputStream)
        {
            return ComputeChecksum(hash, inputStream, 0);
        }

        public static string ComputeChecksum(HashingFunction hash, Stream inputStream, long startPosition)
        {
            if (hash == HashingFunction.None)
            {
                throw new ArgumentOutOfRangeException("hash", "must contain a value other than 'Unknown'");
            }

            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("must be readable.  The CanRead property must return a value of 'true'.", "inputStream");
            }

            if (!inputStream.CanSeek)
            {
                throw new ArgumentException("must be seekable.  The CanSeek property must return a value of 'true'.", "inputStream");
            }

            if (startPosition < 0)
            {
                throw new ArgumentOutOfRangeException("startPosition", "must contain a value greater than or equal to 0");
            }

            HashAlgorithm hashAlgo = null;

            switch (hash)
            {
                case HashingFunction.Crc32:
                    hashAlgo = new Communications.Cryptography.Crc32();
                    break;
                case HashingFunction.Md5:
                    hashAlgo = new MD5CryptoServiceProvider();
                    break;
                case HashingFunction.Sha1:
                    hashAlgo = new SHA1CryptoServiceProvider();
                    break;
            }

            if (startPosition > 0)
            {
                inputStream.Position = startPosition;
            }
            else
            {
                inputStream.Position = 0;
            }

            byte[] hashArray = hashAlgo.ComputeHash(inputStream);
            StringBuilder buffer = new StringBuilder(hashArray.Length);

            foreach (byte hashByte in hashArray)
            {
                buffer.Append(hashByte.ToString("x2"));
            }

            return buffer.ToString();
        }

        internal BackgroundWorker AsyncWorker
        {
            get
            {
                return _asyncWorker;
            }
        }

        public bool IsAsyncCanceled
        {
            get
            {
                return _asyncCanceled;
            }
        }

        public bool IsBusy
        {
            get
            {
                return _asyncWorker == null ? false : _asyncWorker.IsBusy;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                if (this.IsConnected)
                {
                    throw new FtpException("Port property value can not be changed when connection is open.");
                }

                _port = value;
            }
        }

        public string Host
        {
            get
            {
                return _host;
            }

            set
            {
                if (this.IsConnected)
                {
                    throw new FtpException("Host property value can not be changed when connection is open.");
                }

                _host = value;
            }
        }

        public FtpSecurityProtocol SecurityProtocol
        {
            get
            {
                return _securityProtocol;
            }

            set
            {
                if (this.IsConnected)
                {
                    throw new FtpException("SecurityProtocol property value can not be changed when connection is open.");
                }

                _securityProtocol = value;
            }
        }

        public X509CertificateCollection SecurityCertificates
        {
            get
            {
                return _clientCertificates;
            }
        }

        public bool IsCompressionEnabled
        {
            get
            {
                return _isCompressionEnabled;
            }

            set
            {
                if (this.IsBusy)
                {
                    throw new FtpException("IsCompressionEnabled property value can not be changed when the system is busy.");
                }

                try
                {
                    if (this.IsConnected && value && value != _isCompressionEnabled)
                    {
                        CompressionOn();
                    }

                    if (this.IsConnected && !value && value != _isCompressionEnabled)
                    {
                        CompressionOff();
                    }
                }
                catch (FtpException ex)
                {
                    throw new FtpDataCompressionException("An error occurred while trying to enable or disable FTP data compression.", ex);
                }

                _isCompressionEnabled = value;
            }
        }

        public int MaxUploadSpeed
        {
            get
            {
                return _maxUploadSpeed;
            }

            set
            {
                if (value * 1024 > Int32.MaxValue || value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "The MaxUploadSpeed property must have a range of 0 to 2,097,152.");
                }

                _maxUploadSpeed = value;
            }
        }

        public int MaxDownloadSpeed
        {
            get
            {
                return _maxDownloadSpeed;
            }

            set
            {
                if (value * 1024 > Int32.MaxValue || value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "must have a range of 0 to 2,097,152.");
                }

                _maxDownloadSpeed = value;
            }
        }

        public FtpResponse LastResponse
        {
            get
            {
                return _response;
            }
        }

        public FtpResponseCollection LastResponseList
        {
            get
            {
                return _responseList;
            }
        }

        public int TcpBufferSize
        {
            get
            {
                return _tcpBufferSize;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", "must be greater than 0.");
                }

                _tcpBufferSize = value;
            }
        }

        public int TcpTimeout
        {
            get
            {
                return _tcpTimeout;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "must be greater than or equal to 0.");
                }

                _tcpTimeout = value;
            }
        }

        public int TransferTimeout
        {
            get
            {
                return _transferTimeout;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", "must be greater than 0.");
                }

                _transferTimeout = value;
            }
        }

        public int CommandTimeout
        {
            get
            {
                return _commandTimeout;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", "must be greater than 0.");
                }

                _commandTimeout = value;
            }
        }

        public int ActivePortRangeMin
        {
            get
            {
                return _activePortRangeMin;
            }

            set
            {
                if (value > _activePortRangeMin)
                {
                    throw new ArgumentOutOfRangeException("value", "must be less than the ActivePortRangeMax value.");
                }

                if (value < 1 || value > 65534)
                {
                    throw new ArgumentOutOfRangeException("value", "must be between 1 and 65534.");
                }

                if (this.IsBusy)
                {
                    throw new FtpException("ActivePortRangeMin property value can not be changed when the component is busy.");
                }

                _activePortRangeMin = value;
            }
        }

        public int ActivePortRangeMax
        {
            get
            {
                return _activePortRangeMax;
            }

            set
            {
                if (value < _activePortRangeMin)
                {
                    throw new ArgumentOutOfRangeException("value", "must be greater than the ActivePortRangeMin value.");
                }

                if (value < 1 || value > 65534)
                {
                    throw new ArgumentOutOfRangeException("value", "must be between 1 and 65534.");
                }

                if (this.IsBusy)
                {
                    throw new FtpException("ActivePortRangeMax property value can not be changed when the component is busy.");
                }

                _activePortRangeMax = value;
            }
        }

        public TransferMode DataTransferMode
        {
            get
            {
                return _dataTransferMode;
            }

            set
            {
                if (this.IsBusy)
                {
                    throw new FtpException("DataTransferMode property value can not be changed when the component is busy.");
                }

                _dataTransferMode = value;
            }
        }

        public IProxyClient Proxy
        {
            get
            {
                return _proxy;
            }

            set
            {
                _proxy = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (_commandConn == null || _commandConn.Client == null)
                {
                    return false;
                }

                Socket client = _commandConn.Client;

                if (!client.Connected)
                {
                    return false;
                }

                bool blockingState = client.Blocking;
                bool connected = true;

                try
                {
                    byte[] tmp = new byte[1];

                    client.Blocking = false;
                    client.Send(tmp, 0, 0);
                }
                catch (SocketException e)
                {
                    if (!e.NativeErrorCode.Equals(10035))
                    {
                        connected = false;
                    }
                }
                catch (ObjectDisposedException)
                {
                    connected = false;
                }
                finally
                {
                    try
                    {
                        client.Blocking = blockingState;
                    }
                    catch
                    {
                        connected = false;
                    }
                }

                return connected;
            }
        }

        public HashingFunction AutoChecksumValidation
        {
            get
            {
                return _hashAlgorithm;
            }

            set
            {
                _hashAlgorithm = value;
            }
        }

        public Encoding CharacterEncoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("CharacterEncoding");
                }

                _encoding = value;
            }
        }

        internal void SendRequest(FtpRequest request)
        {
            if (_commandConn == null || _commandConn.Connected == false)
            {
                throw new FtpConnectionClosedException("Connection is closed.");
            }

            DontWaitForHappyCodes();

            if (ClientRequest != null)
            {
                ClientRequest(this, new FtpRequestEventArgs(request));
            }

            byte[] buffer = request.GetBytes();

            try
            {
                _commandStream.Write(buffer, 0, buffer.Length);
            }
            catch (IOException ex)
            {
                throw new FtpConnectionBrokenException("Connection is broken.  Failed to send command.", ex);
            }

            if (request.HasHappyCodes)
            {
                WaitForHappyCodes(request.GetHappyCodes());
            }
            else
            {
                if (request.Command != FtpCmd.Quit)
                {
                    Thread.Sleep(2000);
                }

                DontWaitForHappyCodes();
            }
        }

        private void DontWaitForHappyCodes()
        {
            if (_responseQueue.Count == 0)
            {
                return;
            }

            _responseList.Clear();

            while (_responseQueue.Count > 0)
            {
                FtpResponse response = _responseQueue.Dequeue();
                _responseList.Add(response);
                RaiseServerResponseEvent(new FtpResponse(response));
            }

            _response = _responseList.GetLast();
        }

        internal void CreateAsyncWorker()
        {
            if (_asyncWorker != null)
            {
                _asyncWorker.Dispose();
            }

            _asyncWorker = null;
            _asyncCanceled = false;
            _asyncWorker = new BackgroundWorker();
        }

        internal void CloseAllConnections()
        {
            CloseDataConn();
            CloseCommandConn();
            AbortMonitorThread();
        }

        private void AbortMonitorThread()
        {
            _responseMonitor.Abort();
        }

        internal void OpenCommandConn()
        {
            CreateCommandConnection();
            StartCommandMonitorThread();

            if (_securityProtocol == FtpSecurityProtocol.Ssl2Explicit || _securityProtocol == FtpSecurityProtocol.Ssl3Explicit || _securityProtocol == FtpSecurityProtocol.Tls1Explicit || _securityProtocol == FtpSecurityProtocol.Tls1OrSsl3Explicit)
            {
                CreateSslExplicitCommandStream();
            }

            if (_securityProtocol == FtpSecurityProtocol.Ssl2Implicit || _securityProtocol == FtpSecurityProtocol.Ssl3Implicit || _securityProtocol == FtpSecurityProtocol.Tls1Implicit || _securityProtocol == FtpSecurityProtocol.Tls1OrSsl3Implicit)
            {
                CreateSslImplicitCommandStream();
            }

            if (IsAsyncCancellationPending())
            {
                return;
            }

            if (_securityProtocol == FtpSecurityProtocol.None)
            {
                WaitForHappyCodes(FtpResponseCode.ServiceReadyForNewUser);
            }
        }

        internal void TransferData(TransferDirection direction, FtpRequest request, Stream data)
        {
            TransferData(direction, request, data, 0);
        }

        internal void TransferData(TransferDirection direction, FtpRequest request, Stream data, long restartPosition)
        {
            if (_commandConn == null || _commandConn.Connected == false)
            {
                throw new FtpConnectionClosedException("Connection is closed.");
            }

            if (request == null)
            {
                throw new ArgumentNullException("request", "value is required");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data", "value is required");
            }

            switch (direction)
            {
                case TransferDirection.ToClient:
                    if (!data.CanWrite)
                        throw new FtpDataTransferException("Data transfer error.  Data conn does not allow write operation.");
                    break;
                case TransferDirection.ToServer:
                    if (!data.CanRead)
                        throw new FtpDataTransferException("Data transfer error.  Data conn does not allow read operation.");
                    break;
            }

            if (restartPosition > 0 && !data.CanSeek)
            {
                throw new FtpDataTransferException("Data transfer restart error.  Data conn does not allow seek operation.");
            }

            try
            {
                OpenDataConn();

                if (restartPosition > 0)
                {
                    SendRequest(new FtpRequest(_encoding, FtpCmd.Rest, restartPosition.ToString(CultureInfo.InvariantCulture)));
                    data.Position = restartPosition;
                }

                SendRequest(request);
                System.Threading.Thread.Sleep(5);
                WaitForDataConn();

                if (_dataConn == null)
                {
                    throw new FtpDataConnectionException("Unable to establish a data connection to the destination.  The destination may have refused the connection.");
                }

                Stream conn = _dataConn.GetStream();

                if (_securityProtocol != FtpSecurityProtocol.None)
                {
                    conn = CreateSslStream(conn);
                }

                if (_isCompressionEnabled)
                {
                    conn = CreateZlibStream(direction, conn);
                }

                switch (direction)
                {
                    case TransferDirection.ToClient:
                        TransferBytes(conn, data, _maxDownloadSpeed * 1024);
                        break;
                    case TransferDirection.ToServer:
                        TransferBytes(data, conn, _maxUploadSpeed * 1024);
                        break;
                }
            }
            finally
            {
                CloseDataConn();
            }

            WaitForHappyCodes(FtpResponseCode.ClosingDataConnection);

            if (_hashAlgorithm != HashingFunction.None && request.IsFileTransfer)
            {
                DoIntegrityCheck(request, data, restartPosition);
            }
        }

        private Stream CreateZlibStream(TransferDirection direction, Stream stream)
        {
            DeflateStream deflateStream = null;

            switch (direction)
            {
                case TransferDirection.ToClient:
                    deflateStream = new DeflateStream(stream, CompressionMode.Decompress, true);
                    deflateStream.BaseStream.ReadByte();
                    deflateStream.BaseStream.ReadByte();
                    break;
                case TransferDirection.ToServer:
                    deflateStream = new DeflateStream(stream, CompressionMode.Compress, true);
                    deflateStream.BaseStream.WriteByte(120);
                    deflateStream.BaseStream.WriteByte(218);
                    break;
            }

            stream = deflateStream;
            return stream;
        }

        internal string TransferText(FtpRequest request)
        {
            Stream output = new MemoryStream();
            TransferData(TransferDirection.ToClient, request, output);
            output.Position = 0;
            StreamReader reader = new StreamReader(output, _encoding);
            return reader.ReadToEnd();
        }

        internal void CompressionOn()
        {
            try
            {
                SendRequest(new FtpRequest(_encoding, FtpCmd.Mode, "Z"));
            }
            catch (Exception ex)
            {
                throw new FtpDataCompressionException("Unable to enable compression (MODE Z) on the destination.", ex);
            }
        }

        internal void CompressionOff()
        {
            try
            {
                SendRequest(new FtpRequest(_encoding, FtpCmd.Mode, "S"));
            }
            catch (Exception ex)
            {
                throw new FtpDataCompressionException("Unable to disable compression (MODE S) on the destination.", ex);
            }
        }

        private void StartCommandMonitorThread()
        {
            _responseMonitor = new Thread(new ThreadStart(MonitorCommandConnection));
            _responseMonitor.Name = "FtpBase Response Monitor";
            _responseMonitor.Start();
        }

        private bool IsAsyncCancellationPending()
        {
            if (_asyncWorker != null && _asyncWorker.CancellationPending)
            {
                _asyncCanceled = true;
                return true;
            }

            return false;
        }

        private void TransferBytes(Stream input, Stream output, int maxBytesPerSecond)
        {
            int bufferSize = _tcpBufferSize > maxBytesPerSecond && maxBytesPerSecond != 0 ? maxBytesPerSecond : _tcpBufferSize;
            byte[] buffer = new byte[bufferSize];
            long bytesTotal = 0;
            int bytesRead = 0;
            DateTime start = DateTime.Now;
            TimeSpan elapsed = new TimeSpan(0);
            int bytesPerSec = 0;

            while (true)
            {
                bytesRead = input.Read(buffer, 0, bufferSize);

                if (bytesRead == 0)
                {
                    break;
                }

                bytesTotal += bytesRead;
                output.Write(buffer, 0, bytesRead);
                elapsed = DateTime.Now.Subtract(start);
                bytesPerSec = (int)(elapsed.TotalSeconds < 1 ? bytesTotal : bytesTotal / elapsed.TotalSeconds);

                if (TransferProgress != null)
                {
                    TransferProgress(this, new TransferProgressEventArgs(bytesRead, bytesPerSec, elapsed));
                }

                if (IsAsyncCancellationPending())
                {
                    throw new FtpAsynchronousOperationException("Asynchronous operation canceled by user.");
                }

                ThrottleByteTransfer(maxBytesPerSecond, bytesTotal, elapsed, bytesPerSec);
            };

            if (TransferComplete != null)
            {
                TransferComplete(this, new TransferCompleteEventArgs(bytesTotal, bytesPerSec, elapsed));
            }
        }

        private void ThrottleByteTransfer(int maxBytesPerSecond, long bytesTotal, TimeSpan elapsed, int bytesPerSec)
        {
            if (maxBytesPerSecond > 0)
            {
                if (bytesPerSec > maxBytesPerSecond)
                {
                    double elapsedMilliSec = elapsed.TotalSeconds == 0 ? elapsed.TotalMilliseconds : elapsed.TotalSeconds * 1000;
                    double millisecDelay = (bytesTotal / (maxBytesPerSecond / 1000) - elapsedMilliSec);

                    if (millisecDelay > Int32.MaxValue)
                    {
                        millisecDelay = Int32.MaxValue;
                    }

                    Thread.Sleep((int)millisecDelay);
                }
            }
        }

        private void CreateCommandConnection()
        {
            if (_host == null || _host.Length == 0)
            {
                throw new FtpException("An FTP Host must be specified before opening connection to FTP destination.  Set the appropriate value using the Host property on the FtpClient object.");
            }

            try
            {
                if (_proxy != null)
                {
                    _commandConn = _proxy.CreateConnection(_host, _port);
                }
                else
                {
                    _commandConn = new TcpClient(_host, _port);
                }
            }
            catch (ProxyException pex)
            {
                if (_commandConn != null)
                {
                    _commandConn.Close();
                }

                throw new FtpProxyException(String.Format(CultureInfo.InvariantCulture, "A proxy error occurred while creating connection to FTP destination {0} on port {1}.", _host, _port.ToString(CultureInfo.InvariantCulture)), pex);
            }
            catch (Exception ex)
            {
                if (_commandConn != null)
                {
                    _commandConn.Close();
                }

                throw new FtpConnectionOpenException(String.Format(CultureInfo.InvariantCulture, "An error occurred while opening a connection to FTP destination {0} on port {1}.", _host, _port.ToString(CultureInfo.InvariantCulture)), ex);
            }

            _commandConn.ReceiveBufferSize = _tcpBufferSize;
            _commandConn.ReceiveTimeout = _tcpTimeout;
            _commandConn.SendBufferSize = _tcpBufferSize;
            _commandConn.SendTimeout = _tcpTimeout;
            _commandStream = _commandConn.GetStream();
        }

        private void CloseCommandConn()
        {
            if (_commandConn == null)
            {
                return;
            }

            try
            {
                if (_commandConn.Connected)
                {
                    SendRequest(new FtpRequest(_encoding, FtpCmd.Quit));
                }

                _commandConn.Close();
            }
            catch
            {
            }

            _commandConn = null;
        }


        private void WaitForHappyCodes(params FtpResponseCode[] happyResponseCodes)
        {
            WaitForHappyCodes(_commandTimeout, happyResponseCodes);
        }

        internal protected void WaitForHappyCodes(int timeout, params FtpResponseCode[] happyResponseCodes)
        {
            _responseList.Clear();

            do
            {
                FtpResponse response = GetNextCommandResponse(timeout);
                _responseList.Add(response);
                RaiseServerResponseEvent(new FtpResponse(response));

                if (!response.IsInformational)
                {
                    if (IsHappyResponse(response, happyResponseCodes))
                    {
                        break;
                    }

                    if (IsUnhappyResponse(response))
                    {
                        _response = response;
                        throw new FtpResponseException("FTP command failed.", response);
                    }
                }
            } while (true);

            _response = _responseList.GetLast();
        }

        private void RaiseServerResponseEvent(FtpResponse response)
        {
            if (ServerResponse != null)
            {
                ServerResponse(this, new FtpResponseEventArgs(response));
            }
        }

        private void RaiseConnectionClosedEvent()
        {
            if (ConnectionClosed != null)
            {
                ConnectionClosed(this, new ConnectionClosedEventArgs());
            }
        }

        private bool IsUnhappyResponse(FtpResponse response)
        {
            if (response.Code == FtpResponseCode.ServiceNotAvailableClosingControlConnection || response.Code == FtpResponseCode.CannotOpenDataConnection || response.Code == FtpResponseCode.ConnectionClosedSoTransferAborted || response.Code == FtpResponseCode.RequestedFileActionNotTaken || response.Code == FtpResponseCode.RequestedActionAbortedDueToLocalErrorInProcessing || response.Code == FtpResponseCode.RequestedActionNotTakenInsufficientStorage || response.Code == FtpResponseCode.SyntaxErrorCommandUnrecognized || response.Code == FtpResponseCode.SyntaxErrorInParametersOrArguments || response.Code == FtpResponseCode.CommandNotImplemented || response.Code == FtpResponseCode.BadSequenceOfCommands || response.Code == FtpResponseCode.CommandNotImplementedForThatParameter || response.Code == FtpResponseCode.NotLoggedIn || response.Code == FtpResponseCode.NeedAccountForStoringFiles || response.Code == FtpResponseCode.RequestedActionNotTakenFileUnavailable || response.Code == FtpResponseCode.RequestedActionAbortedPageTypeUnknown || response.Code == FtpResponseCode.RequestedFileActionAbortedExceededStorageAllocation || response.Code == FtpResponseCode.RequestedActionNotTakenFileNameNotAllowed)
                return true;
            else
                return false;
        }

        private bool IsHappyResponse(FtpResponse response, FtpResponseCode[] happyResponseCodes)
        {
            if (happyResponseCodes.Length == 0)
            {
                return true;
            }

            for (int j = 0; j < happyResponseCodes.Length; j++)
            {
                if (happyResponseCodes[j] == response.Code)
                {
                    return true;
                }
            }

            return false;
        }

        private void MonitorCommandConnection()
        {
            byte[] buffer = new byte[_tcpBufferSize];
            StringBuilder response = new StringBuilder();

            while (IsConnected)
            {
                lock (_reponseMonitorLock)
                {
                    Thread.Sleep(WAIT_FOR_COMMAND_RESPONSE_INTERVAL);

                    try
                    {
                        if (_commandConn != null && _commandConn.GetStream().DataAvailable)
                        {
                            int bytes = _commandStream.Read(buffer, 0, _tcpBufferSize);
                            string partial = _encoding.GetString(buffer, 0, bytes);
                            response.Append(partial);

                            if (!partial.EndsWith("\r\n"))
                            {
                                continue;
                            }

                            string[] responseArray = SplitResponse(response.ToString());

                            for (int i = 0; i < responseArray.Length; i++)
                            {
                                _responseQueue.Enqueue(new FtpResponse(responseArray[i]));
                            }

                            response.Remove(0, response.Length);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            RaiseConnectionClosedEvent();
        }

        private FtpResponse GetNextCommandResponse(int timeout)
        {
            int sleepTime = 0;

            while (_responseQueue.Count == 0)
            {
                if (!IsConnected)
                {
                    throw new FtpConnectionClosedException("Connection is closed.");
                }

                if (IsAsyncCancellationPending())
                {
                    throw new FtpAsynchronousOperationException("Asynchronous operation canceled.");
                }

                Thread.Sleep(WAIT_FOR_DATA_INTERVAL);
                sleepTime += WAIT_FOR_DATA_INTERVAL;

                if (sleepTime > timeout)
                {
                    throw new FtpCommandResponseTimeoutException("A timeout occurred while waiting for the destination to send a response.  The last reponse from the destination is '" + _response.Text + "'");
                }
            }

            return _responseQueue.Dequeue();
        }

        private string[] SplitResponse(string response)
        {
            return response.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private int GetNextActiveModeListenerPort()
        {
            if (_activePort < _activePortRangeMin || _activePort > _activePortRangeMax)
            {
                _activePort = _activePortRangeMin;
            }
            else
            {
                _activePort++;
            }

            return _activePort;
        }

        private void CreateActiveConn()
        {
            string localHost = Dns.GetHostName();
            IPAddress[] localAddresses = Dns.GetHostAddresses(localHost);
            IPAddress localAddr = null;

            foreach (IPAddress addr in localAddresses)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    localAddr = addr;
                }
            }

            if (localAddr == null)
            {
                throw new Exception("Local host does not have an IPv4 address");
            }

            _activeSignal.Reset();

            bool success = false;
            int listenerPort = 0;

            do
            {
                int failureCnt = 0;

                try
                {
                    listenerPort = GetNextActiveModeListenerPort();
                    _activeListener = new TcpListener(localAddr, listenerPort);
                    _activeListener.Start();
                    success = true;
                }
                catch (SocketException socketError)
                {
                    if (socketError.ErrorCode == 10048 && ++failureCnt < _activePortRangeMax - _activePortRangeMin)
                        _activeListener.Stop();
                    else
                        throw new FtpDataConnectionException(String.Format(CultureInfo.InvariantCulture, "An error occurred while trying to create an active connection on host {0} port {1}", localHost, listenerPort.ToString(CultureInfo.InvariantCulture)), socketError);
                }
            } while (!success);

            byte[] addrBytes = localAddr.GetAddressBytes();
            string dataPortInfo = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5}", addrBytes[0].ToString(CultureInfo.InvariantCulture), addrBytes[1].ToString(CultureInfo.InvariantCulture), addrBytes[2].ToString(CultureInfo.InvariantCulture), addrBytes[3].ToString(CultureInfo.InvariantCulture), listenerPort / 256, listenerPort % 256);

            _activeListener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptTcpClientCallback), _activeListener);

            try
            {
                SendRequest(new FtpRequest(_encoding, FtpCmd.Port, dataPortInfo));
            }
            catch (FtpException fex)
            {
                throw new FtpDataConnectionException(String.Format("An error occurred while issuing data port command '{0}' on an active FTP connection.", dataPortInfo), fex);
            }
        }

        private void AcceptTcpClientCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;

            try
            {
                _dataConn = listener.EndAcceptTcpClient(ar);
            }
            catch
            {
            }

            _activeSignal.Set();
        }

        private void OpenDataConn()
        {
            if (_dataTransferMode == TransferMode.Active)
            {
                CreateActiveConn();
            }
            else
            {
                CreatePassiveConn();
            }
        }

        private void CloseDataConn()
        {
            if (_dataConn != null)
            {
                try
                {
                    _dataConn.Close();
                }
                catch
                {
                }

                _dataConn = null;
            }

            if (_dataTransferMode == TransferMode.Active && _activeListener != null)
            {
                try
                {
                    _activeListener.Stop();
                }
                catch
                {
                }

                _activeListener = null;
            }
        }

        private void WaitForDataConn()
        {
            if (_dataTransferMode == TransferMode.Active)
            {
                if (!_activeSignal.WaitOne(_transferTimeout, false))
                {
                    if (_response.Code == FtpResponseCode.CannotOpenDataConnection)
                    {
                        throw new FtpDataConnectionException(String.Format(CultureInfo.InvariantCulture, "The ftp destination was unable to open a data connection to the ftp client on port {0}.", _activePort));
                    }
                    else
                    {
                        throw new FtpDataConnectionTimeoutException("The data connection timed out waiting for data to transfer from the destination.");
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void CreatePassiveConn()
        {
            try
            {
                SendRequest(new FtpRequest(_encoding, FtpCmd.Pasv));
            }
            catch (FtpException fex)
            {
                throw new FtpDataConnectionException("An error occurred while issuing up a passive FTP connection command.", fex);
            }

            int startIdx = _response.Text.IndexOf("(") + 1;
            int endIdx = _response.Text.IndexOf(")");
            string[] data = _response.Text.Substring(startIdx, endIdx - startIdx).Split(',');
            string passiveHost = data[0] + "." + data[1] + "." + data[2] + "." + data[3];
            int passivePort = Int32.Parse(data[4], CultureInfo.InvariantCulture) * 256 + Int32.Parse(data[5], CultureInfo.InvariantCulture);

            try
            {
                if (_proxy != null)
                {
                    _dataConn = _proxy.CreateConnection(passiveHost, passivePort);
                }
                else
                {
                    _dataConn = new TcpClient(passiveHost, passivePort);
                }

                _dataConn.ReceiveBufferSize = _tcpBufferSize;
                _dataConn.ReceiveTimeout = _tcpTimeout;
                _dataConn.SendBufferSize = _tcpBufferSize;
                _dataConn.SendTimeout = _tcpTimeout;
            }
            catch (Exception ex)
            {
                throw new FtpDataConnectionException(String.Format(CultureInfo.InvariantCulture, "An error occurred while opening passive data connection to destination '{0}' on port '{1}'.", passiveHost, passivePort), ex);
            }
        }

        private Stream CreateSslStream(Stream stream)
        {
            SslStream ssl = new SslStream(stream, true, new RemoteCertificateValidationCallback(secureStream_ValidateServerCertificate), null);
            SslProtocols protocol = SslProtocols.None;

            switch (_securityProtocol)
            {
                case FtpSecurityProtocol.Tls1OrSsl3Explicit:
                case FtpSecurityProtocol.Tls1OrSsl3Implicit:
                    protocol = SslProtocols.Default;
                    break;
                case FtpSecurityProtocol.Ssl2Explicit:
                case FtpSecurityProtocol.Ssl2Implicit:
                    protocol = SslProtocols.Ssl2;
                    break;
                case FtpSecurityProtocol.Ssl3Explicit:
                case FtpSecurityProtocol.Ssl3Implicit:
                    protocol = SslProtocols.Ssl3;
                    break;
                case FtpSecurityProtocol.Tls1Explicit:
                case FtpSecurityProtocol.Tls1Implicit:
                    protocol = SslProtocols.Tls;
                    break;
                default:
                    throw new FtpSecureConnectionException(String.Format("Unhandled FtpSecurityProtocol type '{0}'.", _securityProtocol.ToString()));
            }

            try
            {
                ssl.AuthenticateAsClient(_host, _clientCertificates, protocol, true);
            }
            catch (AuthenticationException authEx)
            {
                throw new FtpAuthenticationException("Secure FTP session certificate authentication failed.", authEx);
            }

            return ssl;
        }

        private bool secureStream_ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_serverCertificate != null && certificate.GetCertHashString() == _serverCertificate.GetCertHashString())
            {
                return true;
            }

            if (ValidateServerCertificate != null)
            {
                ValidateServerCertificateEventArgs args = new ValidateServerCertificateEventArgs(new X509Certificate2(certificate.GetRawCertData()), chain, sslPolicyErrors);
                ValidateServerCertificate(this, args);

                if (args.IsCertificateValid)
                {
                    _serverCertificate = new X509Certificate2(certificate.GetRawCertData());
                }

                return args.IsCertificateValid;
            }
            else
            {
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    throw new FtpCertificateValidationException(String.Format("Certificate validation failed.  The host name '{0}' does not match the name on the security certificate '{1}'.  To override this behavior, subscribe to the ValidateServerCertificate event to validate certificates.", _host, certificate.Issuer));
                }

                if (sslPolicyErrors == SslPolicyErrors.None || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    _serverCertificate = new X509Certificate2(certificate.GetRawCertData());
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void CreateSslExplicitCommandStream()
        {
            try
            {
                string authCommand = "";

                switch (_securityProtocol)
                {
                    case FtpSecurityProtocol.Tls1OrSsl3Explicit:
                    case FtpSecurityProtocol.Ssl3Explicit:
                    case FtpSecurityProtocol.Ssl2Explicit:
                        authCommand = "SSL";
                        break;
                    case FtpSecurityProtocol.Tls1Explicit:
                        authCommand = "TLS";
                        break;
                }

                Debug.Assert(authCommand.Length > 0, "auth command should have a value - make sure every enum option in auth command has a corresponding value");
                SendRequest(new FtpRequest(_encoding, FtpCmd.Auth, authCommand));

                lock (_reponseMonitorLock)
                {
                    _commandStream = CreateSslStream(_commandConn.GetStream());
                }

                SendRequest(new FtpRequest(_encoding, FtpCmd.Pbsz, "0"));
                SendRequest(new FtpRequest(_encoding, FtpCmd.Prot, "P"));
            }
            catch (FtpAuthenticationException fauth)
            {
                throw new FtpSecureConnectionException(String.Format("An ftp authentication exception occurred while setting up a explicit ssl/tls command stream.  {0}", fauth.Message), _response, fauth);
            }
            catch (FtpException fex)
            {
                throw new FtpSecureConnectionException(String.Format("An error occurred while setting up a explicit ssl/tls command stream.  {0}", fex.Message), _response, fex);
            }
        }

        private void CreateSslImplicitCommandStream()
        {
            try
            {
                lock (_reponseMonitorLock)
                {
                    _commandStream = CreateSslStream(_commandConn.GetStream());
                }
            }

            catch (FtpAuthenticationException fauth)
            {
                throw new FtpSecureConnectionException(String.Format("An ftp authentication exception occurred while setting up a implicit ssl/tls command stream.  {0}", fauth.Message), _response, fauth);
            }
            catch (FtpException fex)
            {
                throw new FtpSecureConnectionException(String.Format("An error occurred while setting up a implicit ssl/tls command stream.  {0}", fex.Message), _response, fex);
            }
        }

        private void DoIntegrityCheck(FtpRequest request, Stream stream, long restartPosition)
        {
            string path = request.Arguments[0];
            long startPos = restartPosition;
            long endPos = stream.Length;
            string streamHash = ComputeChecksum(_hashAlgorithm, stream, startPos);
            string serverHash = GetChecksum(_hashAlgorithm, path, startPos, endPos);

            if (String.Compare(streamHash, serverHash, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                throw new FtpFileIntegrityException(String.Format("File integrity check failed.  The destination integrity value '{0}' for the file '{1}' did not match the data transfer integrity value '{2}'.", serverHash, path, streamHash));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_asyncWorker != null && _asyncWorker.IsBusy)
                {
                    _asyncWorker.CancelAsync();
                }

                if (_activeListener != null)
                {
                    _activeListener.Stop();
                }

                if (_dataConn != null && _dataConn.Connected)
                {
                    _dataConn.Close();
                }

                if (_commandConn != null && _commandConn.Connected)
                {
                    _commandConn.Close();
                }

                if (_activeSignal != null)
                {
                    _activeSignal.Close();
                }

                if (_responseMonitor != null && _responseMonitor.IsAlive)
                {
                    _responseMonitor.Abort();
                }
            }
        }

        ~FtpBase()
        {
            Dispose(false);
        }
    }
}
