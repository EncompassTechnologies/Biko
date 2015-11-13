using Communications.Net.Imap.Collections;
using Communications.Net.Imap.Enums;

namespace Communications.Net.Imap
{
    public class GMailMessageThread
    {
        internal GMailMessageThread()
        { }

        internal GMailMessageThread(ImapClient client, Folder folder, long threadId)
        {
            Id = threadId;
            Messages = new MessageCollection(client, folder);
        }

        public long Id
        {
            get;
            private set;
        }

        public MessageCollection Messages
        {
            get;
            set;
        }

        public void FetchAssocicatedMessages(MessageFetchMode mode = MessageFetchMode.ClientDefault, int count = -1)
        {
            Messages.Download("X-GM-THRID " + Id, mode, count);
        }
    }
}