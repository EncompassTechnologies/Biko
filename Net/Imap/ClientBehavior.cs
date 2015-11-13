using Communications.Net.Imap.Constants;
using Communications.Net.Imap.Enums;
using System;

namespace Communications.Net.Imap
{
    public class ClientBehavior
    {
        private MessageFetchMode _messageFetchMode;

        public ClientBehavior()
        {
            FolderTreeBrowseMode = FolderTreeBrowseMode.Lazy;
            MessageFetchMode = MessageFetchMode.Basic;
            ExamineFolders = true;
            AutoPopulateFolderMessages = false;
            FolderDelimeter = '\0';
            SpecialUseMetadataPath = "/private/specialuse";
            AutoDownloadBodyOnAccess = true;
            RequestedHeaders = MessageHeaderSets.Minimal;
            AutoGenerateMissingBody = false;
            SearchAllNotSupported = false;
            NoopIssueTimeout = 840;
        }

        public FolderTreeBrowseMode FolderTreeBrowseMode
        {
            get;
            set;
        }

        public MessageFetchMode MessageFetchMode
        {
            get
            {
                return _messageFetchMode;
            }
            set
            {
                if (value == MessageFetchMode.ClientDefault)
                {
                    throw new ArgumentException("The default fetch mode cannot be set to ClientDefault!");
                }

                _messageFetchMode = value;
            }
        }

        public string[] RequestedHeaders
        {
            get;
            set;
        }

        public bool AutoPopulateFolderMessages
        {
            get;
            set;
        }

        public bool AutoDownloadBodyOnAccess
        {
            get;
            set;
        }

        public bool ExamineFolders
        {
            get;
            set;
        }

        internal char FolderDelimeter
        {
            get;
            set;
        }

        public string SpecialUseMetadataPath
        {
            get;
            set;
        }

        public bool AutoGenerateMissingBody
        {
            get;
            set;
        }

        public bool SearchAllNotSupported
        {
            get;
            set;
        }

        public bool LazyFolderBrowsingNotSupported
        {
            get;
            set;
        }

        public int NoopIssueTimeout
        {
            get;
            set;
        }
    }
}