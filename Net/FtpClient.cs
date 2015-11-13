using System;
using System.IO;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Security.Permissions;
using System.Collections.Generic;
using Communications.Net.Ftp;

namespace Communications.Net
{
    public class FtpClient : FtpBase
    {
        public FtpClient()
            : base(DEFAULT_FTP_PORT, FtpSecurityProtocol.None)
        {
        }

        public FtpClient(string host)
            : this(host, DEFAULT_FTP_PORT, FtpSecurityProtocol.None)
        {
        }

        public FtpClient(string host, int port)
            : base(host, port, FtpSecurityProtocol.None)
        {
        }

        public FtpClient(string host, int port, FtpSecurityProtocol securityProtocol)
            : base(host, port, securityProtocol)
        {
        }

        private const int DEFAULT_FTP_PORT = 21;
        private const int FXP_TRANSFER_TIMEOUT = 600000;
        private TransferType _fileTransferType = TransferType.Binary;
        private IFtpItemParser _itemParser;
        private string _user;
        private string _password;
        private bool _opened;
        private string _currentDirectory;
        private int _fxpTransferTimeout = FXP_TRANSFER_TIMEOUT;
        private Stream _log = new MemoryStream();
        private bool _isLoggingOn;

        public TransferType FileTransferType
        {
            get
            {
                return _fileTransferType;
            }

            set
            {
                _fileTransferType = value;

                if (this.IsConnected == true)
                {
                    SetFileTransferType();
                }
            }
        }

        public IFtpItemParser ItemParser
        {
            get
            {
                return _itemParser;
            }

            set
            {
                _itemParser = value;
            }
        }

        public bool IsLoggingOn
        {
            get
            {
                return _isLoggingOn;
            }

            set
            {
                _isLoggingOn = value;
            }
        }

        public Stream Log
        {
            get
            {
                return _log;
            }

            set
            {
                if (((Stream)value).CanWrite == false)
                {
                    throw new ArgumentException("must be writable. The property CanWrite must have a value equals to 'true'.", "value");
                }

                _log = value;
            }
        }

        public int FxpTransferTimeout
        {
            get
            {
                return _fxpTransferTimeout;
            }

            set
            {
                _fxpTransferTimeout = value;
            }
        }

        public string CurrentDirectory
        {
            get
            {
                return _currentDirectory;
            }
        }

        public void Open(string user, string password)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user", "must have a value");
            }

            if (user.Length == 0)
            {
                throw new ArgumentException("must have a value", "user");
            }

            if (password == null)
            {
                throw new ArgumentNullException("password", "must have a value or an empty string");
            }

            if (!this.IsConnected)
            {
                base.OpenCommandConn();
            }

            if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
            {
                base.CloseAllConnections();
                return;
            }

            _user = user;
            _password = password;
            _currentDirectory = "/";

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.User, user));
            }
            catch (FtpException fex)
            {
                throw new FtpConnectionOpenException(String.Format("An error occurred when sending user information.  Reason: {0}", base.LastResponse.Text), fex);
            }

            Thread.Sleep(500);

            if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
            {
                base.CloseAllConnections();
                return;
            }

            if (base.LastResponse.Code != FtpResponseCode.UserLoggedIn)
            {
                try
                {
                    base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Pass, password));
                }
                catch (FtpException fex)
                {
                    throw new FtpConnectionOpenException(String.Format("An error occurred when sending password information.  Reason: {0}", base.LastResponse.Text), fex);
                }

                if (base.LastResponse.Code == FtpResponseCode.NotLoggedIn)
                {
                    throw new FtpLoginException("Unable to log into FTP destination with supplied username and password.");
                }
            }

            if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
            {
                base.CloseAllConnections();
                return;
            }

            if (_itemParser == null)
            {
                _itemParser = new FtpGenericParser();
            }

            SetFileTransferType();

            if (base.IsCompressionEnabled)
            {
                base.CompressionOn();
            }

            if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
            {
                base.CloseAllConnections();
                return;
            }

            _opened = true;
        }

        public void Reopen()
        {
            if (!_opened)
            {
                throw new FtpException("You must use the Open() method before using the Reopen() method.");
            }

            Open(_user, _password);
        }

        public void ChangeUser(string user, string password)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user", "must have a value");
            }

            if (user.Length == 0)
            {
                throw new ArgumentException("must have a value", "user");
            }

            if (password == null)
            {
                throw new ArgumentNullException("password", "must have a value");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.User, user));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred when sending user information.", base.LastResponse, fex);
            }

            Thread.Sleep(500);

            if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
            {
                base.CloseAllConnections();
                return;
            }

            if (base.LastResponse.Code != FtpResponseCode.UserLoggedIn)
            {
                try
                {
                    base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Pass, password));
                }
                catch (FtpException fex)
                {
                    throw new FtpException("An error occurred when sending password information.", base.LastResponse, fex);
                }

                if (base.LastResponse.Code == FtpResponseCode.NotLoggedIn)
                {
                    throw new FtpLoginException("Unable to log into FTP destination with supplied username and password.");
                }
            }
        }

        public void Close()
        {
            base.CloseAllConnections();
        }

        public void ChangeDirectoryMultiPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            path = path.Replace("\\", "/");
            string[] dirs = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                foreach (string dir in dirs)
                {
                    base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Cwd, dir));
                }
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("Could not change working directory to '{0}'.", path), fex);
            }

            _currentDirectory = GetWorkingDirectory();
        }

        public void ChangeDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            path = path.Replace("\\", "/");

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Cwd, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("Could not change working directory to '{0}'.", path), fex);
            }

            _currentDirectory = GetWorkingDirectory();
        }

        public string GetWorkingDirectory()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Pwd));
            }
            catch (FtpException fex)
            {
                throw new FtpException("Could not retrieve working directory.", base.LastResponse, fex);
            }

            string dir = base.LastResponse.Text;

            if (dir.Substring(0, 1) == "\"")
            {
                dir = dir.Substring(1, dir.IndexOf("\"", 1) - 1);
            }

            return dir;
        }

        public void DeleteFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Dele, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("Unable to the delete file {0}.", path), base.LastResponse, fex);
            }
        }

        public void Abort()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Abor));
            }
            catch (FtpException fex)
            {
                throw new FtpException("Abort command failed or was unable to be issued.", base.LastResponse, fex);
            }
        }

        public void MakeDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must contain a value", "path");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Mkd, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("The directory {0} could not be created.", path), base.LastResponse, fex);
            }
        }

        public void MoveFile(string fromPath, string toPath)
        {
            if (fromPath == null)
            {
                throw new ArgumentNullException("fromPath");
            }

            if (fromPath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "fromPath");
            }

            if (toPath == null)
            {
                throw new ArgumentNullException("toPath");
            }

            if (fromPath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "toPath");
            }

            MemoryStream fileStream = new MemoryStream();
            GetFile(fromPath, fileStream, false);
            fileStream.Position = 0;
            this.PutFile((Stream)fileStream, toPath, FileAction.Create);
            this.DeleteFile(fromPath);
        }

        public void DeleteDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Rmd, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("The FTP destination was unable to delete the directory '{0}'.", path), base.LastResponse, fex);
            }
        }

        public string GetHelp()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Help));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred while getting the system help.", base.LastResponse, fex);
            }

            return base.LastResponse.Text;
        }

        public DateTime GetFileDateTime(string fileName, bool adjustToLocalTime)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (fileName.Length == 0)
            {
                throw new ArgumentException("must contain a value", "fileName");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Mdtm, fileName));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred when retrieving file date and time for '{0}'.", fileName), base.LastResponse, fex);
            }

            string response = base.LastResponse.Text;
            int year = int.Parse(response.Substring(0, 4), CultureInfo.InvariantCulture);
            int month = int.Parse(response.Substring(4, 2), CultureInfo.InvariantCulture);
            int day = int.Parse(response.Substring(6, 2), CultureInfo.InvariantCulture);
            int hour = int.Parse(response.Substring(8, 2), CultureInfo.InvariantCulture);
            int minute = int.Parse(response.Substring(10, 2), CultureInfo.InvariantCulture);
            int second = int.Parse(response.Substring(12, 2), CultureInfo.InvariantCulture);

            DateTime dateUtc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

            if (adjustToLocalTime)
            {
                return new DateTime(dateUtc.ToLocalTime().Ticks);
            }
            else
            {
                return new DateTime(dateUtc.Ticks);
            }
        }

        public void SetDateTime(string path, DateTime dateTime)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            string dateTimeArg = dateTime.ToString("yyyyMMddHHmmss");

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Mdtm, dateTimeArg, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred when setting a file date and time for '{0}'.", path), fex);
            }
        }

        public string GetStatus()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Stat));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred while getting the system status.", base.LastResponse, fex);
            }

            return base.LastResponse.Text;
        }

        public void ChangeDirectoryUp()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Cdup));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred when changing directory to the parent (ChangeDirectoryUp).", base.LastResponse, fex);
            }
        }

        public long GetFileSize(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Length == 0)
                throw new ArgumentException("must contain a value", "path");

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Size, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred when retrieving file size for {0}.", path), base.LastResponse, fex);
            }

            return Int64.Parse(base.LastResponse.Text, CultureInfo.InvariantCulture);
        }

        public string GetFeatures()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Feat));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred when retrieving features.", base.LastResponse, fex);
            }

            return base.LastResponse.Text;
        }

        public string GetStatus(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must contain a value", "path");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Stat, path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred when retrieving file status for file '{0}'.", path), base.LastResponse, fex);
            }

            return base.LastResponse.Text;
        }

        public void AllocateStorage(long size)
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Stor, size.ToString()));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred when trying to allocate storage on the destination.", base.LastResponse, fex);
            }
        }

        public string GetSystemType()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Syst));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred while getting the system type.", base.LastResponse, fex);
            }

            return base.LastResponse.Text;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFileUnique(string localPath)
        {
            if (localPath == null)
            {
                throw new ArgumentNullException("localPath");
            }

            try
            {
                using (FileStream fileStream = File.OpenRead(localPath))
                {
                    PutFileUnique(fileStream);
                }
            }
            catch (FtpException fex)
            {
                WriteToLog(String.Format("Action='PutFileUnique';Action='TransferError';LocalPath='{0}';CurrentDirectory='{1}';ErrorMessage='{2}'", localPath, _currentDirectory, fex.Message));
                throw new FtpException("An error occurred while executing PutFileUnique() on the remote FTP destination.", base.LastResponse, fex);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFileUnique(Stream inputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("must be readable.  The CanRead property must return a value of 'true'.", "inputStream");
            }

            WriteToLog(String.Format("Action='PutFileUnique';Action='TransferBegin';CurrentDirectory='{0}'", _currentDirectory));

            try
            {
                base.TransferData(TransferDirection.ToServer, new FtpRequest(base.CharacterEncoding, FtpCmd.Stor), inputStream);
            }
            catch (Exception ex)
            {
                WriteToLog(String.Format("Action='PutFileUnique';Action='TransferError';CurrentDirectory='{0}';ErrorMessage='{1}'", _currentDirectory, ex.Message));
                throw new FtpException("An error occurred while executing PutFileUnique() on the remote FTP destination.", base.LastResponse, ex);
            }

            WriteToLog(String.Format("Action='PutFileUnique';Action='TransferSuccess';CurrentDirectory='{0}'", _currentDirectory));
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void GetFile(string remotePath, string localPath)
        {
            GetFile(remotePath, localPath, FileAction.CreateNew);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void GetFile(string remotePath, string localPath, FileAction action)
        {
            if (remotePath == null)
            {
                throw new ArgumentNullException("remotePath");
            }

            if (remotePath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "remotePath");
            }

            if (localPath == null)
            {
                throw new ArgumentNullException("localPath");
            }

            if (localPath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "localPath");
            }

            if (action == FileAction.None)
            {
                throw new ArgumentOutOfRangeException("action", "must contain a value other than 'Unknown'");
            }

            localPath = CorrectLocalPath(localPath);
            WriteToLog(String.Format("Action='GetFile';Status='TransferBegin';LocalPath='{0}';RemotePath='{1}';FileAction='{1}'", localPath, remotePath, action));
            FtpRequest request = new FtpRequest(base.CharacterEncoding, FtpCmd.Retr, remotePath);

            try
            {
                switch (action)
                {
                    case FileAction.CreateNew:
                        using (Stream localFile = File.Open(localPath, FileMode.CreateNew))
                        {
                            base.TransferData(TransferDirection.ToClient, request, localFile);
                        }

                        break;
                    case FileAction.Create:
                        using (Stream localFile = File.Open(localPath, FileMode.Create))
                        {
                            base.TransferData(TransferDirection.ToClient, request, localFile);
                        }

                        break;
                    case FileAction.CreateOrAppend:
                        using (Stream localFile = File.Open(localPath, FileMode.OpenOrCreate))
                        {
                            localFile.Position = localFile.Length;
                            base.TransferData(TransferDirection.ToClient, request, localFile);
                        }

                        break;
                    case FileAction.Resume:
                        using (Stream localFile = File.Open(localPath, FileMode.Open))
                        {
                            long remoteSize = GetFileSize(remotePath);

                            if (localFile.Length == remoteSize)
                            {
                                return;
                            }

                            base.TransferData(TransferDirection.ToClient, request, localFile, localFile.Length - 1);
                        }

                        break;
                    case FileAction.ResumeOrCreate:
                        if (File.Exists(localPath) && (new FileInfo(localPath)).Length > 0)
                        {
                            GetFile(remotePath, localPath, FileAction.Resume);
                        }
                        else
                        {
                            GetFile(remotePath, localPath, FileAction.Create);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                WriteToLog(String.Format("Action='GetFile';Status='TransferError';LocalPath='{0}';RemotePath='{1}';FileAction='{1}';ErrorMessage='{2}", localPath, remotePath, action, ex.Message));
                throw new FtpException(String.Format("An unexpected exception occurred while retrieving file '{0}'.", remotePath), base.LastResponse, ex);
            }

            WriteToLog(String.Format("Action='GetFile';Status='TransferSuccess';LocalPath='{0}';RemotePath='{1}';FileAction='{1}'", localPath, remotePath, action));
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void GetFile(string remotePath, Stream outStream, bool restart)
        {
            if (remotePath == null)
            {
                throw new ArgumentNullException("remotePath");
            }

            if (remotePath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "remotePath");
            }

            if (outStream == null)
            {
                throw new ArgumentNullException("outStream");
            }

            if (outStream.CanWrite == false)
            {
                throw new ArgumentException("must be writable.  The CanWrite property must return the value 'true'.", "outStream");
            }

            FtpRequest request = new FtpRequest(base.CharacterEncoding, FtpCmd.Retr, remotePath);

            if (restart)
            {
                long remoteSize = GetFileSize(remotePath);

                if (outStream.Length == remoteSize)
                {
                    return;
                }

                base.TransferData(TransferDirection.ToClient, request, outStream, outStream.Length - 1);
            }
            else
            {
                base.TransferData(TransferDirection.ToClient, request, outStream);
            }
        }

        public bool Exists(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException("must have a value", "path");
            }

            path = path.Replace("\\", "/");
            string fname = Path.GetFileName(path);

            string[] dirs = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (dirs.Length == 1)
            {
                return Exists("/", fname);
            }
            else
            {
                return Exists(path.Substring(0, path.Length - (fname.Length)), fname);
            }
        }

        public bool Exists(string path, string filename)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException("must have a value", "path");
            }

            if (String.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("must have a value", "filename");
            }

            path = path.Replace("\\", "/");

            int dirCount = 0;
            bool found = false;

            string[] dirs = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                if (path != "/")
                {
                    foreach (string dir in dirs)
                    {
                        ChangeDirectory(dir);
                        dirCount++;
                    }
                }

                found = GetDirList().ContainsName(filename);
            }
            catch (FtpException)
            {
            }
            finally
            {
                for (int j = 0; j < dirCount; j++)
                {
                    this.ChangeDirectoryUp();
                }
            }

            return found;
        }

        public string GetNameList()
        {
            return base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.Nlst));
        }

        public string GetNameList(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.Nlst, path));
        }

        public string GetDirListAsText()
        {
            return base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.List, "-al"));
        }

        public string GetDirListAsText(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.List, "-al", path));
        }

        public FtpItemCollection GetDirList()
        {
            return new FtpItemCollection("*", _currentDirectory, base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.List, "-al")), _itemParser);
        }

        public FtpItemCollection GetDirList(string fileMask)
        {
            if (fileMask == null)
            {
                throw new ArgumentNullException("fileMask");
            }

            return new FtpItemCollection(fileMask, _currentDirectory, base.TransferText(new FtpRequest(base.CharacterEncoding, FtpCmd.List, "-al")), _itemParser);
        }

        public FtpItemCollection GetDirListDeep(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            FtpItemCollection deepCol = new FtpItemCollection();
            ParseDirListDeep(path, deepCol);
            return deepCol;
        }

        public void Rename(string name, string newName)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "must have a value");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("must have a value", "name");
            }

            if (newName == null)
            {
                throw new ArgumentNullException("newName", "must have a value");
            }

            if (newName.Length == 0)
            {
                throw new ArgumentException("must have a value", "newName");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Rnfr, name));
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Rnto, newName));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("The FTP destination was unable to rename the file or directory '{0}' to the new name '{1}'.", name, newName), base.LastResponse, fex);
            }
        }

        public string Quote(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (command.Length < 3)
            {
                throw new ArgumentException(String.Format("Invalid command '{0}'.", command), "command");
            }

            char[] separator = { ' ' };
            string[] values = command.Split(separator);
            string code;

            if (values.Length == 0)
            {
                code = command;
            }
            else
            {
                code = values[0];
            }

            string args = string.Empty;

            if (command.Length > code.Length)
            {
                args = command.Replace(code, "").TrimStart();
            }

            FtpCmd ftpCmd = FtpCmd.Unknown;

            try
            {
                ftpCmd = (FtpCmd)Enum.Parse(typeof(FtpCmd), code, true);
            }
            catch
            {
                ftpCmd = FtpCmd.Unknown;
            }

            if (ftpCmd == FtpCmd.Pasv || ftpCmd == FtpCmd.Retr || ftpCmd == FtpCmd.Stor || ftpCmd == FtpCmd.Stou || ftpCmd == FtpCmd.Erpt || ftpCmd == FtpCmd.Epsv)
            {
                throw new ArgumentException(String.Format("Command '{0}' not supported by Quote() method.", code), "command");
            }

            if (ftpCmd == FtpCmd.List || ftpCmd == FtpCmd.Nlst)
            {
                return base.TransferText(new FtpRequest(base.CharacterEncoding, ftpCmd, args));
            }

            if (ftpCmd == FtpCmd.Unknown)
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, ftpCmd, command));
            }
            else
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, ftpCmd, args));
            }

            return base.LastResponseList.GetRawText();
        }

        public void NoOperation()
        {
            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Noop));
            }
            catch (FtpException fex)
            {
                throw new FtpException("An error occurred while issuing the No Operation command (NOOP).", base.LastResponse, fex);
            }
        }

        public void ChangeMode(string path, int octalValue)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            try
            {
                base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Site, "CHMOD", octalValue.ToString(), path));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("Unable to the change file mode for file {0}.  Reason: {1}", path, base.LastResponse.Text), base.LastResponse, fex);
            }

            if (base.LastResponse.Code == FtpResponseCode.CommandNotImplementedSuperfluousAtThisSite)
            {
                throw new FtpException(String.Format("Unable to the change file mode for file {0}.  Reason: {1}", path, base.LastResponse.Text), base.LastResponse);
            }
        }

        public void Site(string argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException("argument", "must have a value");
            }

            if (argument.Length == 0)
            {
                throw new ArgumentException("must have a value", "argument");
            }

            base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Site, argument));
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFile(string localPath, string remotePath, FileAction action)
        {
            using (FileStream fileStream = File.OpenRead(localPath))
            {
                PutFile(fileStream, remotePath, action);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFile(string localPath, string remotePath)
        {
            using (FileStream fileStream = File.OpenRead(localPath))
            {
                PutFile(fileStream, remotePath, FileAction.CreateNew);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFile(string localPath, FileAction action)
        {
            using (FileStream fileStream = File.OpenRead(localPath))
            {
                PutFile(fileStream, ExtractPathItemName(localPath), action);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFile(string localPath)
        {
            using (FileStream fileStream = File.OpenRead(localPath))
            {
                PutFile(fileStream, ExtractPathItemName(localPath), FileAction.CreateNew);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void PutFile(Stream inputStream, string remotePath, FileAction action)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }

            if (!inputStream.CanRead)
            {
                throw new ArgumentException("must be readable", "inputStream");
            }

            if (remotePath == null)
            {
                throw new ArgumentNullException("remotePath");
            }

            if (remotePath.Length == 0)
            {
                throw new ArgumentException("must contain a value", "remotePath");
            }

            if (action == FileAction.None)
            {
                throw new ArgumentOutOfRangeException("action", "must contain a value other than 'Unknown'");
            }

            WriteToLog(String.Format("Action='PutFile';Status='TransferBegin';RemotePath='{0}';FileAction='{1}'", remotePath, action));

            try
            {
                switch (action)
                {
                    case FileAction.CreateOrAppend:
                        base.TransferData(TransferDirection.ToServer, new FtpRequest(base.CharacterEncoding, FtpCmd.Appe, remotePath), inputStream);
                        break;
                    case FileAction.CreateNew:
                        if (Exists(remotePath))
                        {
                            throw new FtpException("Cannot overwrite existing file when action FileAction.CreateNew is specified.");
                        }

                        base.TransferData(TransferDirection.ToServer, new FtpRequest(base.CharacterEncoding, FtpCmd.Stor, remotePath), inputStream);
                        break;
                    case FileAction.Create:
                        base.TransferData(TransferDirection.ToServer, new FtpRequest(base.CharacterEncoding, FtpCmd.Stor, remotePath), inputStream);
                        break;
                    case FileAction.Resume:
                        long remoteSize = GetFileSize(remotePath);

                        if (remoteSize == inputStream.Length)
                        {
                            return;
                        }

                        base.TransferData(TransferDirection.ToServer, new FtpRequest(base.CharacterEncoding, FtpCmd.Stor, remotePath), inputStream, remoteSize);
                        break;
                    case FileAction.ResumeOrCreate:
                        if (Exists(remotePath))
                        {
                            PutFile(inputStream, remotePath, FileAction.Resume);
                        }
                        else
                        {
                            PutFile(inputStream, remotePath, FileAction.Create);
                        }

                        break;
                }
            }
            catch (FtpException fex)
            {
                WriteToLog(String.Format("Action='PutFile';Status='TransferError';RemotePath='{0}';FileAction='{1}';ErrorMessage='{2}'", remotePath, action, fex.Message));
                throw new FtpDataTransferException(String.Format("An error occurred while putting fileName '{0}'.", remotePath), base.LastResponse, fex);
            }

            WriteToLog(String.Format("Action='PutFile';Status='TransferSuccess';RemotePath='{0}';FileAction='{1}'", remotePath, action));
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void FxpCopy(string fileName, FtpClient destination)
        {
            if (this.IsConnected == false)
            {
                throw new FtpException("The connection must be open before a transfer between servers can be initiated.");
            }

            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            if (destination.IsConnected == false)
            {
                throw new FtpException("The destination object must be open and connected before a transfer between servers can be initiated.");
            }

            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (fileName.Length == 0)
            {
                throw new ArgumentException("must have a value", "fileName");
            }

            try
            {
                destination.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Pasv));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred when trying to set up the passive connection on '{1}' for a destination to destination copy between '{0}' and '{1}'.", this.Host, destination.Host), base.LastResponse, fex);
            }

            int startIdx = destination.LastResponse.Text.IndexOf("(") + 1;
            int endIdx = destination.LastResponse.Text.IndexOf(")");
            string dataPortInfo = destination.LastResponse.Text.Substring(startIdx, endIdx - startIdx);

            try
            {
                this.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Port, dataPortInfo));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("Command instructing '{0}' to open connection failed.", this.Host), base.LastResponse, fex);
            }

            try
            {
                this.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Retr, fileName));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred transferring on a server to server copy between '{0}' and '{1}'.", this.Host, destination.Host), base.LastResponse, fex);
            }

            try
            {
                destination.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Stor, fileName));
            }
            catch (FtpException fex)
            {
                throw new FtpException(String.Format("An error occurred transferring on a server to server copy between '{0}' and '{1}'.", this.Host, destination.Host), base.LastResponse, fex);
            }

            destination.WaitForHappyCodes(this.FxpTransferTimeout, FtpResponseCode.RequestedFileActionOkayAndCompleted, FtpResponseCode.ClosingDataConnection);
            this.WaitForHappyCodes(this.FxpTransferTimeout, FtpResponseCode.RequestedFileActionOkayAndCompleted, FtpResponseCode.ClosingDataConnection);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void ParseDirListDeep(string path, FtpItemCollection deepCol)
        {
            FtpItemCollection itemCol = GetDirList(path);
            deepCol.Merge(itemCol);

            foreach (FtpItem item in itemCol)
            {
                if (base.AsyncWorker != null && base.AsyncWorker.CancellationPending)
                {
                    return;
                }

                if (item.ItemType == FtpItemType.Directory)
                {
                    ParseDirListDeep(item.FullPath, deepCol);
                }
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private string CorrectLocalPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException("must have a value", "path");
            }

            string fileName = ExtractPathItemName(path);
            string pathOnly = path.Substring(0, path.Length - fileName.Length - 1);

            if (pathOnly.EndsWith(":") && pathOnly.IndexOf("\\") == -1)
            {
                pathOnly += "\\";
            }

            char[] invalidPath = Path.GetInvalidPathChars();

            if (path.IndexOfAny(invalidPath) != -1)
            {
                for (int i = 0; i < invalidPath.Length; i++)
                {
                    if (pathOnly.IndexOf(invalidPath[i]) != -1)
                    {
                        pathOnly = pathOnly.Replace(invalidPath[i], '_');
                    }
                }
            }

            char[] invalidFile = Path.GetInvalidFileNameChars();

            if (fileName.IndexOfAny(invalidFile) != -1)
            {
                for (int i = 0; i < invalidFile.Length; i++)
                {
                    if (fileName.IndexOf(invalidFile[i]) != -1)
                    {
                        fileName = fileName.Replace(invalidFile[i], '_');
                    }
                }
            }

            return Path.Combine(pathOnly, fileName);
        }

        private void SetFileTransferType()
        {
            switch (_fileTransferType)
            {
                case TransferType.Binary:
                    base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Type, "I"));
                    break;
                case TransferType.Ascii:
                    base.SendRequest(new FtpRequest(base.CharacterEncoding, FtpCmd.Type, "A"));
                    break;
            }
        }

        private string ExtractPathItemName(string path)
        {
            if (path.IndexOf("\\") != -1)
            {
                return path.Substring(path.LastIndexOf("\\") + 1);
            }
            else if (path.IndexOf("/") != -1)
            {
                return path.Substring(path.LastIndexOf("/") + 1);
            }
            else if (path.Length > 0)
            {
                return path;
            }
            else
            {
                throw new FtpException(String.Format(CultureInfo.InvariantCulture, "Item name not found in path {0}.", path));
            }
        }

        private void WriteToLog(string message)
        {
            if (!_isLoggingOn)
            {
                return;
            }

            string line = String.Format("[{0}] [{1}] [{2}] {3}\r\n", DateTime.Now.ToString("G"), base.Host, base.Port, message);
            byte[] buffer = base.CharacterEncoding.GetBytes(line);
            _log.Write(buffer, 0, buffer.Length);
        }

        private Exception _asyncException;
        public event EventHandler<GetDirListAsyncCompletedEventArgs> GetDirListAsyncCompleted;

        public void GetDirListAsync()
        {
            GetDirListAsync(string.Empty);
        }

        public void GetDirListAsync(string path)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(GetDirListAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GetDirListAsync_RunWorkerCompleted);
            base.AsyncWorker.RunWorkerAsync(path);
        }

        private void GetDirListAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string path = (string)e.Argument;
                e.Result = GetDirList(path);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void GetDirListAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (GetDirListAsyncCompleted != null)
            {
                GetDirListAsyncCompleted(this, new GetDirListAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled, (FtpItemCollection)e.Result));
            }

            _asyncException = null;
        }

        public event EventHandler<GetDirListDeepAsyncCompletedEventArgs> GetDirListDeepAsyncCompleted;

        public void GetDirListDeepAsync()
        {
            GetDirListDeepAsync(GetWorkingDirectory());
        }

        public void GetDirListDeepAsync(string path)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(GetDirListDeepAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GetDirListDeepAsync_RunWorkerCompleted);
            base.AsyncWorker.RunWorkerAsync(path);
        }

        private void GetDirListDeepAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string path = (string)e.Argument;
                e.Result = GetDirList(path);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void GetDirListDeepAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (GetDirListDeepAsyncCompleted != null)
            {
                GetDirListDeepAsyncCompleted(this, new GetDirListDeepAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled, (FtpItemCollection)e.Result));
            }

            _asyncException = null;
        }

        public event EventHandler<GetFileAsyncCompletedEventArgs> GetFileAsyncCompleted;

        public void GetFileAsync(string remotePath, string localPath, FileAction action)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(GetFileAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GetFileAsync_RunWorkerCompleted);
            Object[] args = new Object[3];
            args[0] = remotePath;
            args[1] = localPath;
            args[2] = action;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void GetFileAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                GetFile((string)args[0], (string)args[1], (FileAction)args[2]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void GetFileAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (GetFileAsyncCompleted != null)
            {
                GetFileAsyncCompleted(this, new GetFileAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled));
            }

            _asyncException = null;
        }

        public void GetFileAsync(string remotePath, Stream outStream, bool restart)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(GetFileStreamAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GetFileAsync_RunWorkerCompleted);
            Object[] args = new Object[3];
            args[0] = remotePath;
            args[1] = outStream;
            args[2] = restart;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void GetFileStreamAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                GetFile((string)args[0], (Stream)args[1], (bool)args[2]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        public event EventHandler<PutFileAsyncCompletedEventArgs> PutFileAsyncCompleted;

        public void PutFileAsync(string localPath, string remotePath, FileAction action)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(PutFileAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PutFileAsync_RunWorkerCompleted);
            Object[] args = new Object[3];
            args[0] = localPath;
            args[1] = remotePath;
            args[2] = action;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void PutFileAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                PutFile((string)args[0], (string)args[1], (FileAction)args[2]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void PutFileAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (PutFileAsyncCompleted != null)
            {
                PutFileAsyncCompleted(this, new PutFileAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled));
            }

            _asyncException = null;
        }

        public void PutFileAsync(Stream inputStream, string remotePath, FileAction action)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(PutFileStreamAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PutFileAsync_RunWorkerCompleted);
            Object[] args = new Object[3];
            args[0] = inputStream;
            args[1] = remotePath;
            args[2] = action;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void PutFileStreamAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                PutFile((Stream)args[0], (string)args[1], (FileAction)args[2]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        public void PutFileAsync(string localPath, FileAction action)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(PutFileLocalAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PutFileAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = localPath;
            args[1] = action;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void PutFileLocalAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                PutFile((string)args[0], (FileAction)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        public event EventHandler<OpenAsyncCompletedEventArgs> OpenAsyncCompleted;

        public void OpenAsync(string user, string password)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(OpenAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OpenAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = user;
            args[1] = password;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void OpenAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                Open((string)args[0], (string)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void OpenAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (OpenAsyncCompleted != null)
            {
                OpenAsyncCompleted(this, new OpenAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled));
            }

            _asyncException = null;
        }

        public event EventHandler<FxpCopyAsyncCompletedEventArgs> FxpCopyAsyncCompleted;

        public void FxpCopyAsync(string fileName, FtpClient destination)
        {
            if (base.AsyncWorker != null && base.AsyncWorker.IsBusy)
            {
                throw new InvalidOperationException("The FtpConnection object is already busy executing another asynchronous operation.  You can only execute one asynchronous method at a time.");
            }

            base.CreateAsyncWorker();
            base.AsyncWorker.WorkerSupportsCancellation = true;
            base.AsyncWorker.DoWork += new DoWorkEventHandler(FxpCopyAsync_DoWork);
            base.AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FxpCopyAsync_RunWorkerCompleted);
            Object[] args = new Object[2];
            args[0] = fileName;
            args[1] = destination;
            base.AsyncWorker.RunWorkerAsync(args);
        }

        private void FxpCopyAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Object[] args = (Object[])e.Argument;
                FxpCopy((string)args[0], (FtpClient)args[1]);
            }
            catch (Exception ex)
            {
                _asyncException = ex;
            }
        }

        private void FxpCopyAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (FxpCopyAsyncCompleted != null)
            {
                FxpCopyAsyncCompleted(this, new FxpCopyAsyncCompletedEventArgs(_asyncException, base.IsAsyncCanceled));
            }

            _asyncException = null;
        }

        new protected virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        override protected void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        ~FtpClient()
        {
            Dispose(false);
        }
    }
}
