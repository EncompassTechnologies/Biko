using Communications.Net.Imap.Constants;
using Communications.Net.Imap.Enums;
using Communications.Net.Imap.Exceptions;
using Communications.Net.Imap.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Communications.Net.Imap
{
    public class ImapBase : IDisposable
    {
        public const int DefaultImapPort = 143;
        public const int DefaultImapSslPort = 993;
        protected static readonly Regex NewLineRex = new Regex(@"\r\n");

        protected TcpClient _client;
        protected long _counter;

        protected string _host;
        protected Stream _ioStream;
        protected object _lock = 0;
        protected int _port = DefaultImapPort;

        protected SslProtocols _sslProtocol = SslProtocols.None;
        protected StreamReader _streamReader;
        protected bool _validateServerCertificate = true;
        protected DateTime _lastActivity;

        public ClientBehavior Behavior
        {
            get;
            protected set;
        }

        public bool IsAuthenticated
        {
            get;
            protected set;
        }

        public bool IsConnected
        {
            get
            {
                return _client != null && _client.Connected;
            }
        }

        public bool IsDebug
        {
            get;
            set;
        }

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                if (IsConnected)
                {
                    throw new InvalidStateException("The host cannot be changed after the connection has been established. Please disconnect first.");
                }

                _host = value;
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
                if (IsConnected)
                {
                    throw new InvalidStateException("The port cannot be changed after the connection has been established. Please disconnect first.");
                }

                _port = value;
            }
        }

        internal Folder SelectedFolder
        {
            get;
            set;
        }

        public SslProtocols SslProtocol
        {
            get
            {
                return _sslProtocol;
            }
            set
            {
                if (IsConnected)
                {
                    throw new InvalidStateException("The SSL protocol cannot be changed after the connection has been established. Please disconnect first.");
                }

                _sslProtocol = value;
            }
        }

        public bool UseSsl
        {
            get
            {
                return _sslProtocol != SslProtocols.None;
            }
            set
            {
                _sslProtocol = value ? SslProtocols.Default : SslProtocols.None;
            }
        }

        public bool ValidateServerCertificate
        {
            get
            {
                return _validateServerCertificate;
            }
            set
            {
                if (IsConnected)
                {
                    throw new InvalidStateException("The certificate validation mode cannot be changed after the connection has been established. Please disconnect first.");
                }

                _validateServerCertificate = value;
            }
        }

        public Capability Capabilities
        {
            get;
            protected set;
        }

        public void Dispose()
        {
            CleanUp();
        }

        public bool Connect()
        {
            return Connect(_host, _port, _sslProtocol, _validateServerCertificate);
        }

        public bool Connect(string host, bool useSsl = false, bool validateServerCertificate = true)
        {
            return Connect(host, useSsl ? DefaultImapSslPort : DefaultImapPort, useSsl ? SslProtocols.Default : SslProtocols.None, validateServerCertificate);
        }

        public bool Connect(string host, int port, bool useSsl = false, bool validateServerCertificate = true)
        {
            return Connect(host, port, useSsl ? SslProtocols.Default : SslProtocols.None, validateServerCertificate);
        }

        public bool Connect(string host, int port, SslProtocols sslProtocol = SslProtocols.None,
            bool validateServerCertificate = true)
        {
            _host = host;
            _port = port;
            _sslProtocol = sslProtocol;
            _validateServerCertificate = validateServerCertificate;

            if (IsConnected)
            {
                throw new InvalidStateException("The client is already connected. Please disconnect first.");
            }

            try
            {
                _client = new TcpClient(_host, _port);

                if (_sslProtocol == SslProtocols.None)
                {
                    _ioStream = _client.GetStream();
                    _streamReader = new StreamReader(_ioStream);
                }
                else
                {
                    _ioStream = new SslStream(_client.GetStream(), false, CertificateValidationCallback, null);
                    (_ioStream as SslStream).AuthenticateAsClient(_host, null, _sslProtocol, false);
                    _streamReader = new StreamReader(_ioStream);
                }

                string result = _streamReader.ReadLine();
                _lastActivity = DateTime.Now;

                if (result != null && result.StartsWith(ResponseType.ServerOk))
                {
                    Capability();
                    return true;
                }
                else if (result != null && result.StartsWith(ResponseType.ServerPreAuth))
                {
                    IsAuthenticated = true;
                    Capability();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (!IsConnected)
                {
                    CleanUp();
                }
            }
        }

        public void Disconnect()
        {
            CleanUp();
        }

        protected void CleanUp()
        {
            _counter = 0;

            if (!IsConnected)
            {
                return;
            }

            StopIdling();

            if (_streamReader != null)
            {
                _streamReader.Dispose();
            }

            if (_ioStream != null)
            {
                _ioStream.Dispose();
            }

            if (_client != null)
            {
                _client.Close();
            }
        }

        protected bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None || !_validateServerCertificate;
        }

        protected void Capability()
        {
            IList<string> data = new List<string>();

            if (SendAndReceive(ImapCommands.Capability, ref data) && data.Count > 0)
            {
                Capabilities = new Capability(data[0]);
            }
        }

        public bool SendAndReceive(string command, ref IList<string> data, CommandProcessor processor = null,
            Encoding encoding = null, bool pushResultToDatadespiteProcessor = false)
        {
            if (_idleState == IdleState.On)
            {
                PauseIdling();
            }

            lock (_lock)
            {
                if (_client == null || !_client.Connected)
                {
                    throw new SocketException((int)SocketError.NotConnected);
                }

                const string tmpl = "IMAP{0} {1}";
                _counter++;

                StreamReader reader = encoding == null || Equals(encoding, Encoding.UTF8) ? _streamReader : new StreamReader(_ioStream, encoding);
                var parts = new Queue<string>(NewLineRex.Split(command));
                string text = string.Format(tmpl, _counter, parts.Dequeue().Trim()) + "\r\n";
                byte[] bytes = Encoding.UTF8.GetBytes(text.ToCharArray());

                if (IsDebug)
                {
                    Debug.WriteLine(text);
                }

                _ioStream.Write(bytes, 0, bytes.Length);
                _lastActivity = DateTime.Now;

                while (true)
                {
                    string tmp = reader.ReadLine();

                    if (tmp == null)
                    {
                        if (_idleState == IdleState.Paused)
                        {
                            StartIdling();
                        }

                        return false;
                    }

                    if (IsDebug)
                    {
                        Debug.WriteLine(tmp);
                    }

                    if (processor == null || pushResultToDatadespiteProcessor)
                    {
                        data.Add(tmp);
                    }

                    if (processor != null)
                    {
                        processor.ProcessCommandResult(tmp);
                    }

                    if (tmp.StartsWith("+ ") && (parts.Count > 0 || (processor != null && processor.TwoWayProcessing)))
                    {
                        if (parts.Count > 0)
                        {
                            text = parts.Dequeue().Trim() + "\r\n";

                            if (IsDebug)
                            {
                                Debug.WriteLine(text);
                            }

                            bytes = Encoding.UTF8.GetBytes(text.ToCharArray());
                        }
                        else if (processor != null)
                        {
                            bytes = processor.AppendCommandData(tmp);
                        }

                        _ioStream.Write(bytes, 0, bytes.Length);
                        continue;
                    }

                    if (tmp.StartsWith(string.Format(tmpl, _counter, ResponseType.Ok)))
                    {
                        if (_idleState == IdleState.Paused)
                        {
                            StartIdling();
                        }

                        return true;
                    }

                    if (tmp.StartsWith(string.Format(tmpl, _counter, ResponseType.PreAuth)))
                    {
                        if (_idleState == IdleState.Paused)
                        {
                            StartIdling();
                        }

                        return true;
                    }

                    if (tmp.StartsWith(string.Format(tmpl, _counter, ResponseType.No)) || tmp.StartsWith(string.Format(tmpl, _counter, ResponseType.Bad)))
                    {
                        if (_idleState == IdleState.Paused)
                        {
                            StartIdling();
                        }

                        var serverAlertMatch = Expressions.ServerAlertRex.Match(tmp);

                        if (serverAlertMatch.Success && tmp.Contains("IMAP") && tmp.Contains("abled"))
                        {
                            throw new ServerAlertException(serverAlertMatch.Groups[1].Value);
                        }
                        return false;
                    }
                }
            }
        }

        private IdleState _idleState;

        internal IdleState IdleState
        {
            get
            {
                return _idleState;
            }
        }

        private Thread _idleLoopThread;
        private Thread _idleProcessThread;
        private Thread _idleNoopIssueThread;
        private long _lastIdleUId;
        private readonly Queue<string> _idleEvents = new Queue<string>();

        internal bool StartIdling()
        {
            if (SelectedFolder == null)
            {
                return false;
            }

            switch (_idleState)
            {
                case IdleState.Off:
                    _lastIdleUId = SelectedFolder.UidNext;
                    break;

                case IdleState.On:
                    return true;

                case IdleState.Paused:
                    _lastIdleUId = SelectedFolder.UidNext;
                    break;
            }

            lock (_lock)
            {
                const string tmpl = "IMAP{0} {1}";
                _counter++;
                string text = string.Format(tmpl, _counter, "IDLE") + "\r\n";
                byte[] bytes = Encoding.UTF8.GetBytes(text.ToCharArray());

                if (IsDebug)
                {
                    Debug.WriteLine(text);
                }

                _ioStream.Write(bytes, 0, bytes.Length);
                string line = "";

                if (_ioStream.ReadByte() != '+')
                {
                    return false;
                }
                else
                {
                    line = _streamReader.ReadLine();
                }

                if (IsDebug)
                {
                    Debug.WriteLine(line);
                }
            }

            _idleState = IdleState.On;
            _idleLoopThread = new Thread(WaitForIdleServerEvents) { IsBackground = true };
            _idleLoopThread.Start();

            if (OnIdleStarted != null)
                OnIdleStarted(SelectedFolder, new IdleEventArgs
                {
                    Client = SelectedFolder.Client,
                    Folder = SelectedFolder
                });

            return true;
        }

        private void MaintainIdleConnection()
        {
            while (true)
            {
                if (_idleState == IdleState.On)
                {
                    var diff = DateTime.Now.Subtract(_lastActivity).TotalSeconds;

                    if (diff >= Behavior.NoopIssueTimeout)
                    {
                        IList<string> data = new List<string>();

                        if (!SendAndReceive(ImapCommands.Noop, ref data))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private void ProcessIdleServerEvents()
        {
            while (true)
            {
                if (_idleEvents.Count == 0)
                {
                    if (_idleState == IdleState.On)
                    {
                        continue;
                    }
                    else
                    {
                        return;
                    }
                }

                var tmp = _idleEvents.Dequeue();
                var match = Expressions.IdleResponseRex.Match(tmp);

                if (!match.Success)
                {
                    continue;
                }

                if (match.Groups[2].Value == "EXISTS")
                {
                    SelectedFolder.Status(new[] { FolderStatusFields.UIdNext });

                    if (_lastIdleUId != SelectedFolder.UidNext)
                    {
                        var msgs = SelectedFolder.Search(string.Format("UID {0}:{1}", _lastIdleUId, SelectedFolder.UidNext));
                        var args = new IdleEventArgs
                        {
                            Folder = SelectedFolder,
                            Messages = msgs
                        };

                        if (OnNewMessagesArrived != null)
                        {
                            OnNewMessagesArrived(SelectedFolder, args);
                        }

                        SelectedFolder.RaiseNewMessagesArrived(args);
                        _lastIdleUId = SelectedFolder.UidNext;
                    }
                }
            }
        }

        private void WaitForIdleServerEvents()
        {
            if (_idleProcessThread == null)
            {
                _idleProcessThread = new Thread(ProcessIdleServerEvents) { IsBackground = true };
                _idleProcessThread.Start();
            }

            if (_idleNoopIssueThread == null)
            {
                _idleNoopIssueThread = new Thread(MaintainIdleConnection) { IsBackground = true };
                _idleNoopIssueThread.Start();
            }

            while (_idleState == IdleState.On)
            {
                if (_ioStream.ReadByte() != -1)
                {
                    string tmp = _streamReader.ReadLine();

                    if (tmp == null)
                    {
                        continue;
                    }

                    if (IsDebug)
                    {
                        Debug.WriteLine(tmp);
                    }

                    if (tmp.ToUpper().Contains("OK"))
                    {
                        _idleState = IdleState.Off;
                        return;
                    }

                    _idleEvents.Enqueue(tmp);

                    if (_idleProcessThread == null)
                    {
                        _idleProcessThread = new Thread(ProcessIdleServerEvents) { IsBackground = true };
                        _idleProcessThread.Start();
                    }
                }
            }
        }

        internal void PauseIdling()
        {
            if (_idleState != IdleState.On)
            {
                return;
            }

            StopIdling(true);
            _idleState = IdleState.Paused;

            if (OnIdlePaused != null)
                OnIdlePaused(SelectedFolder, new IdleEventArgs
                {
                    Client = SelectedFolder.Client,
                    Folder = SelectedFolder
                });
        }

        internal void StopIdling(bool pausing = false)
        {
            if (_idleState == IdleState.Off)
            {
                return;
            }

            _counter++;
            const string text = "DONE" + "\r\n";
            byte[] bytes = Encoding.UTF8.GetBytes(text.ToCharArray());

            if (IsDebug)
            {
                Debug.WriteLine(text);
            }

            _ioStream.Write(bytes, 0, bytes.Length);

            if (!pausing && _idleProcessThread != null)
            {
                _idleProcessThread.Join();
                _idleProcessThread = null;
            }

            if (_idleNoopIssueThread != null)
            {
                _idleNoopIssueThread.Join();
                _idleNoopIssueThread = null;
            }

            if (_idleLoopThread != null)
            {
                _idleLoopThread.Join();
                _idleLoopThread = null;
            }

            if (!pausing && OnIdleStopped != null)
                OnIdleStopped(SelectedFolder, new IdleEventArgs
                {
                    Client = SelectedFolder.Client,
                    Folder = SelectedFolder
                });
        }

        public event EventHandler<IdleEventArgs> OnNewMessagesArrived;

        public event EventHandler<IdleEventArgs> OnIdleStarted;

        public event EventHandler<IdleEventArgs> OnIdlePaused;

        public event EventHandler<IdleEventArgs> OnIdleStopped;
    }
}