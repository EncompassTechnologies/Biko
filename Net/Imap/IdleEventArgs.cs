using System;

namespace Communications.Net.Imap
{
    public class IdleEventArgs : EventArgs
    {
        public ImapClient Client
        {
            get;
            set;
        }

        public Folder Folder
        {
            get;
            set;
        }

        public Message[] Messages
        {
            get;
            set;
        }
    }
}