using Communications.Net.Imap.Enums;

namespace Communications.Net.Imap.Collections
{
    public class MessageCollection : ImapObjectCollection<Message>
    {
        private readonly Folder _folder;

        public MessageCollection(ImapClient client, Folder folder)
            : base(client)
        {
            _folder = folder;
        }

        public void Download(string query = "ALL", MessageFetchMode mode = MessageFetchMode.ClientDefault, int count = -1)
        {
            _folder.Search(query, mode, count);
        }

        public void Download(long[] uIds, MessageFetchMode mode = MessageFetchMode.ClientDefault)
        {
            _folder.Search(uIds, mode);
        }
    }
}