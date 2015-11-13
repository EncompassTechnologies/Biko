using Communications.Net.Imap.Authentication;
using Communications.Net.Imap.Collections;
using Communications.Net.Imap.Constants;
using Communications.Net.Imap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;

namespace Communications.Net.Imap
{
    public class ImapClient : ImapBase
    {
        private CommonFolderCollection _folders;

        public CommonFolderCollection Folders
        {
            get
            {
                return _folders ?? (_folders = GetFolders());
            }
        }

        public ImapCredentials Credentials
        {
            get;
            set;
        }

        public ImapClient()
        {
            Behavior = new ClientBehavior();
        }

        public ImapClient(string host, bool useSsl = false, bool validateServerCertificate = true)
            : this(host, useSsl ? DefaultImapSslPort : DefaultImapPort, useSsl ? SslProtocols.Default : SslProtocols.None, validateServerCertificate)
        { }

        public ImapClient(string host, int port, bool useSsl = false, bool validateServerCertificate = true)
            : this(host, port, useSsl ? SslProtocols.Default : SslProtocols.None, validateServerCertificate)
        { }

        public ImapClient(string host, int port, SslProtocols sslProtocol = SslProtocols.None, bool validateServerCertificate = true)
            : this()
        {
            Host = host;
            Port = port;
            SslProtocol = sslProtocol;
            ValidateServerCertificate = validateServerCertificate;
        }

        public bool Login()
        {
            if (Credentials == null)
            {
                throw new ArgumentNullException("The credentials cannot be null");
            }

            return Login(Credentials);
        }

        public bool Login(string login, string password)
        {
            return Login(new PlainCredentials(login, password));
        }

        public bool Login(ImapCredentials credentials)
        {
            Credentials = credentials;
            IList<string> data = new List<string>();
            IsAuthenticated = SendAndReceive(credentials.ToCommand(Capabilities), ref data, credentials, null, true);

            var capabilities = data.FirstOrDefault(_ => _.StartsWith("* CAPABILITY"));

            if (Capabilities == null)
            {
                Capabilities = new Capability(capabilities);
            }
            else
            {
                Capabilities.Update(capabilities);
            }

            if (IsAuthenticated && Host.ToLower() == "imap.qq.com")
            {
                Behavior.SearchAllNotSupported = true;
                Behavior.LazyFolderBrowsingNotSupported = true;
            }

            return IsAuthenticated;
        }

        public bool Logout()
        {
            IList<string> data = new List<string>();

            if (SendAndReceive(ImapCommands.Logout, ref data))
            {
                IsAuthenticated = false;
                Behavior.FolderDelimeter = '\0';
                _folders = null;
            }

            return !IsAuthenticated;
        }

        internal CommonFolderCollection GetFolders()
        {
            var folders = new CommonFolderCollection(this);
            folders.AddRangeInternal(GetFolders("", folders, null, true));
            return folders;
        }

        internal FolderCollection GetFolders(string path, CommonFolderCollection commonFolders, Folder parent = null, bool isFirstLevel = false)
        {
            var result = new FolderCollection(this, parent);
            var cmd = string.Format(Capabilities.XList && !Capabilities.XGMExt1 ? ImapCommands.XList : ImapCommands.List, path, Behavior.FolderTreeBrowseMode == FolderTreeBrowseMode.Full || (parent != null && Behavior.LazyFolderBrowsingNotSupported) ? "*" : "%");
            IList<string> data = new List<string>();

            if (!SendAndReceive(cmd, ref data))
            {
                return result;
            }

            for (var i = 0; i < data.Count - 1; i++)
            {
                var folder = Folder.Parse(data[i], ref parent, this);
                commonFolders.TryBind(ref folder);

                if (Behavior.ExamineFolders)
                {
                    folder.Examine();
                }

                if (folder.HasChildren && (isFirstLevel || Behavior.FolderTreeBrowseMode == FolderTreeBrowseMode.Full))
                {
                    folder.SubFolders = GetFolders(folder.Path + Behavior.FolderDelimeter, commonFolders, folder);
                }

                result.AddInternal(folder);
            }

            return result;
        }
    }
}