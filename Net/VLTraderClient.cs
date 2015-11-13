using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Linq;

namespace Communications.Net
{
    public class VLTraderClient : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_ftpClient != null)
                {
                    _ftpClient.Dispose();
                    _ftpClient = null;
                }
        }

        ~VLTraderClient()
        {
            Dispose(false);
        }

        public VLTraderClient()
        {
            CreateConnection(true);
        }

        public VLTraderClient(bool secure)
        {
            CreateConnection(secure);
        }

        private void CreateConnection(bool secure)
        {
            if (secure)
            {
                _ftpClient = new FtpClient(Communications.Settings.Url, 990, Ftp.FtpSecurityProtocol.Ssl3Implicit);
                _ftpClient.ValidateServerCertificate += new EventHandler<Communications.Net.Ftp.ValidateServerCertificateEventArgs>(ValidateServerCertificate);
            }
            else
            {
                _ftpClient = new FtpClient(Communications.Settings.Url, 21);
            }

            _ftpClient.FileTransferType = Communications.Net.Ftp.TransferType.Binary;
            _ftpClient.DataTransferMode = Communications.Net.Ftp.TransferMode.Passive;
            _ftpClient.Open(Communications.Settings.UserName, Communications.Settings.Password);
        }

        private FtpClient _ftpClient;

        public void Close()
        {
            _ftpClient.Close();
        }

        public bool IsConnected
        {
            get
            {
                return _ftpClient.IsConnected;
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool PutFile(string mailboxId, string filePath)
        {
            return PutFile(Mailbox.Outbox, mailboxId, filePath);
        }

        private void ValidateServerCertificate(object sender, Communications.Net.Ftp.ValidateServerCertificateEventArgs e)
        {
            e.IsCertificateValid = true;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool MoveFileOutToIn(string mailboxId, string filePath)
        {
            string toDirectory = String.Format("/{0}/{1}", Communications.Settings.Inbox, mailboxId);
            string fromDirectory = String.Format("/{0}/{1}", Communications.Settings.Outbox, mailboxId);

            if (!_ftpClient.Exists(toDirectory))
            {
                _ftpClient.MakeDirectory(toDirectory);
            }

            this.changeDirectorySub(fromDirectory);
            _ftpClient.MoveFile(System.IO.Path.GetFileName(filePath), String.Format("{0}/{1}", toDirectory, System.IO.Path.GetFileName(filePath)));
            return true;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool PutFile(Mailbox mailbox, string mailboxId, string filePath)
        {
            string liveDirectory;
            string transferDirectory;

            switch (mailbox)
            {
                case Mailbox.Inbox:
                    liveDirectory = String.Format("/{0}/{1}", Communications.Settings.Inbox, mailboxId);
                    transferDirectory = String.Format("/{0}/{1}/Transfer", Communications.Settings.Inbox, mailboxId);
                    break;
                case Mailbox.OpenDSDInbox:
                    liveDirectory = String.Format("/{0}/{1}", Communications.Settings.OpenDSDInbox, mailboxId);
                    transferDirectory = String.Format("/{0}/{1}/Transfer", Communications.Settings.OpenDSDInbox, mailboxId);
                    break;
                case Mailbox.OpenDSDOutbox:
                    liveDirectory = String.Format("/{0}/{1}", Communications.Settings.OpenDSDOutbox, mailboxId);
                    transferDirectory = String.Format("/{0}/{1}/Transfer", Communications.Settings.OpenDSDOutbox, mailboxId);
                    break;
                default:
                    liveDirectory = String.Format("/{0}/{1}", Communications.Settings.Outbox, mailboxId);
                    transferDirectory = String.Format("/{0}/{1}/Transfer", Communications.Settings.Outbox, mailboxId);
                    break;
            }

            this.changeDirectorySub(liveDirectory);
            this.changeDirectorySub(transferDirectory);

            if (System.IO.File.Exists(filePath))
            {
                _ftpClient.PutFile(filePath, Communications.Net.Ftp.FileAction.Create);
                this.changeDirectorySub(liveDirectory);
                if (!_ftpClient.Exists(System.IO.Path.GetFileName(filePath)))
                {
                    _ftpClient.Rename(String.Format("Transfer/{0}", System.IO.Path.GetFileName(filePath)), System.IO.Path.GetFileName(filePath));
                }
                else
                {
                    _ftpClient.MoveFile(String.Format("Transfer/{0}", System.IO.Path.GetFileName(filePath)), System.IO.Path.GetFileName(filePath));
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public string[] GetFiles(string mailboxId, string path)
        {
            return GetFiles(Mailbox.Inbox, mailboxId, path);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public string[] GetFiles(Mailbox mailbox, string mailboxId, string path)
        {
            List<string> files = new List<string>();
            Random random = new Random();
            string processKey;

            processKey = DateTime.Now.ToString("ffff") + "-" + random.Next(0, 10000);

            this.changeDirectory(mailbox, mailboxId);

            foreach (string file in GetDirList(mailbox, mailboxId))
            {
                if (GetFile(mailbox, mailboxId, file, path))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }

        public bool GetFile(string mailboxId, string file, string path)
        {
            return GetFile(Mailbox.Inbox, mailboxId, file, path);
        }

        public bool GetFile(Mailbox mailbox, string mailboxId, string file, string path)
        {
            this.changeDirectory(mailbox, mailboxId);

            try
            {
                _ftpClient.Rename(file, file + ".lock");
                _ftpClient.GetFile(file + ".lock", System.IO.Path.Combine(path, file + ".part"), Net.Ftp.FileAction.Create);

                try
                {
                    System.IO.File.Move(System.IO.Path.Combine(path, file + ".part"), System.IO.Path.Combine(path, file));
                    _ftpClient.DeleteFile(file + ".lock");
                    return true;
                }
                catch
                {
                    _ftpClient.Rename(file + ".lock", file);
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public string[] GetDirList(string mailboxId)
        {
            return GetDirList(Mailbox.Inbox, mailboxId);
        }

        public string[] GetDirList(Mailbox mailbox, string mailboxId)
        {
            List<string> fileList = new System.Collections.Generic.List<string>();
            string response;
            char[] crlfSplit = new char[2];

            crlfSplit[0] = '\r';
            crlfSplit[1] = '\n';

            this.changeDirectory(mailbox, mailboxId);

            response = _ftpClient.GetNameList();

            foreach (string file in response.Split(crlfSplit, StringSplitOptions.RemoveEmptyEntries))
            {
                if (System.IO.Path.GetExtension(file) != ".lock" && System.IO.Path.GetExtension(file) != ".part" && file.ToLower() != "transfer" && file.ToLower() != "archive")
                {
                    fileList.Add(file);
                }
            }

            return fileList.ToArray();
        }

        public string GetCurrentMailbox()
        {
            return _ftpClient.GetWorkingDirectory();
        }

        public void DeleteFile(string mailboxId, string fileName)
        {
            DeleteFile(Mailbox.Inbox, mailboxId, fileName);
        }

        public void DeleteFile(Mailbox mailbox, string mailboxId, string file)
        {
            this.changeDirectory(mailbox, mailboxId);

            if (_ftpClient.Exists(file))
            {
                _ftpClient.Rename(file, file + ".lock");
                _ftpClient.DeleteFile(file + ".lock");
            }
        }

        public void changeDirectory(Mailbox mailbox, string mailboxId)
        {
            string mailboxRoot;
            string directory;
            switch (mailbox)
            {
                case Mailbox.Inbox:
                    mailboxRoot = Communications.Settings.Inbox;
                    break;
                case Mailbox.OpenDSDInbox:
                    mailboxRoot = Communications.Settings.OpenDSDInbox;
                    break;
                case Mailbox.OpenDSDOutbox:
                    mailboxRoot = Communications.Settings.OpenDSDOutbox;
                    break;
                default:
                    mailboxRoot = Communications.Settings.Outbox;
                    break;
            }

            directory = String.Format("/{0}/{1}/", mailboxRoot, mailboxId);

            this.changeDirectorySub(directory);
        }

        public void changeDirectorySub(string directory)
        {
            try
            {
                _ftpClient.ChangeDirectory(directory);
            }
            catch (Exception ex)
            {
                try
                {
                    _ftpClient.MakeDirectory(directory);
                    _ftpClient.ChangeDirectory(directory);
                }
                catch (Exception e)
                {
                    try
                    {
                        _ftpClient.ChangeDirectory(directory);
                    }
                    catch (Exception exc)
                    {
                        throw exc;
                    }
                }
            }
        }
    }
}
