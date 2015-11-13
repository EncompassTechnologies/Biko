using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Communications.Net.Ftp
{

    public class FtpRequest
    {
        private FtpCmd _command;
        private string[] _arguments;
        private string _text;
        private Encoding _encoding;

        public FtpRequest()
        {
            _encoding = Encoding.UTF8;
            _command = new FtpCmd();
            _text = string.Empty;
        }

        internal FtpRequest(Encoding encoding, FtpCmd command, params string[] arguments)
        {
            _encoding = encoding;
            _command = command;
            _arguments = arguments;
            _text = BuildCommandText();
        }

        internal FtpRequest(Encoding encoding, FtpCmd command)
            : this(encoding, command, null)
        {
        }

        public FtpCmd Command
        {
            get
            {
                return _command;
            }
        }

        public List<string> Arguments
        {
            get
            {
                return new List<string>(_arguments);
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        public bool IsFileTransfer
        {
            get
            {
                if (_command == FtpCmd.Stou || _command == FtpCmd.Stor || _command == FtpCmd.Retr)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal string BuildCommandText()
        {
            string commandText = _command.ToString().ToUpper(CultureInfo.InvariantCulture);

            if (_arguments == null)
            {
                return commandText;
            }
            else
            {
                StringBuilder builder = new StringBuilder();

                foreach (string arg in _arguments)
                {
                    builder.Append(arg);
                    builder.Append(" ");
                }

                string argText = builder.ToString().TrimEnd();

                if (_command == FtpCmd.Unknown)
                {
                    return argText;
                }
                else
                {
                    return String.Format("{0} {1}", commandText, argText).TrimEnd();
                }
            }
        }

        internal byte[] GetBytes()
        {
            return _encoding.GetBytes(String.Format("{0}\r\n", _text));
        }

        internal bool HasHappyCodes
        {
            get
            {
                return GetHappyCodes().Length == 0 ? false : true;
            }
        }

        internal FtpResponseCode[] GetHappyCodes()
        {
            switch (_command)
            {
                case FtpCmd.Unknown:
                    return BuildResponseArray();
                case FtpCmd.Allo:
                    return BuildResponseArray(FtpResponseCode.CommandOkay, FtpResponseCode.CommandNotImplementedSuperfluousAtThisSite);
                case FtpCmd.User:
                    return BuildResponseArray(FtpResponseCode.UserNameOkayButNeedPassword, FtpResponseCode.ServiceReadyForNewUser, FtpResponseCode.UserLoggedIn);
                case FtpCmd.Pass:
                    return BuildResponseArray(FtpResponseCode.UserLoggedIn, FtpResponseCode.ServiceReadyForNewUser, FtpResponseCode.NotLoggedIn);
                case FtpCmd.Cwd:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Pwd:
                    return BuildResponseArray(FtpResponseCode.PathNameCreated);
                case FtpCmd.Dele:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Abor:
                    return BuildResponseArray();
                case FtpCmd.Mkd:
                    return BuildResponseArray(FtpResponseCode.PathNameCreated);
                case FtpCmd.Rmd:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Help:
                    return BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply, FtpResponseCode.HelpMessage, FtpResponseCode.FileStatus);
                case FtpCmd.Mdtm:
                    return BuildResponseArray(FtpResponseCode.FileStatus);
                case FtpCmd.Stat:
                    return BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply, FtpResponseCode.DirectoryStatus, FtpResponseCode.FileStatus);
                case FtpCmd.Cdup:
                    return BuildResponseArray(FtpResponseCode.CommandOkay, FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Size:
                    return BuildResponseArray(FtpResponseCode.FileStatus);
                case FtpCmd.Feat:
                    return BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply);
                case FtpCmd.Syst:
                    return BuildResponseArray(FtpResponseCode.NameSystemType);
                case FtpCmd.Rnfr:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionPendingFurtherInformation);
                case FtpCmd.Rnto:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Noop:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.Site:
                    return BuildResponseArray(FtpResponseCode.CommandOkay, FtpResponseCode.CommandNotImplementedSuperfluousAtThisSite, FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Pasv:
                    return BuildResponseArray(FtpResponseCode.EnteringPassiveMode);
                case FtpCmd.Port:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.Type:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.Rest:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionPendingFurtherInformation);
                case FtpCmd.Mode:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.Quit:
                    return BuildResponseArray();
                case FtpCmd.Auth:
                    return BuildResponseArray(FtpResponseCode.AuthenticationCommandOkay);
                case FtpCmd.Pbsz:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.Prot:
                    return BuildResponseArray(FtpResponseCode.CommandOkay);
                case FtpCmd.List:
                case FtpCmd.Nlst:
                    return BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting, FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection, FtpResponseCode.ClosingDataConnection, FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Appe:
                case FtpCmd.Stor:
                case FtpCmd.Stou:
                case FtpCmd.Retr:
                    return BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting, FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection, FtpResponseCode.ClosingDataConnection, FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Xcrc:
                case FtpCmd.Xmd5:
                case FtpCmd.Xsha1:
                    return BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted);
                case FtpCmd.Epsv:
                    return BuildResponseArray();
                case FtpCmd.Erpt:
                    return BuildResponseArray();
                default:
                    throw new FtpException(String.Format("No response code(s) defined for FtpCmd {0}.", _command));
            }
        }

        private FtpResponseCode[] BuildResponseArray(params FtpResponseCode[] codes)
        {
            return codes;
        }
    }
}
