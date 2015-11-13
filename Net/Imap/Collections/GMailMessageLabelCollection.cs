using System;
using System.Collections.Generic;

namespace Communications.Net.Imap.Collections
{
    public class GMailMessageLabelCollection : MessageFlagCollection
    {
        public GMailMessageLabelCollection()
        {
        }

        public GMailMessageLabelCollection(Message message)
            : base(message)
        {
        }

        public GMailMessageLabelCollection(ImapClient client, Message message)
            : base(client, message)
        {
            AddType = "+X-GM-LABELS";
            RemoveType = "-X-GM-LABELS";
            IsUTF7 = true;
            AddQuotes = true;
        }

        public new bool Add(string label)
        {
            if (!Client.Capabilities.XGMExt1)
            {
                throw new NotSupportedException("Google Mail labels are not supported on this server!");
            }

            return base.Add(label);
        }

        public new bool AddRange(IEnumerable<string> labels)
        {
            if (!Client.Capabilities.XGMExt1)
            {
                throw new NotSupportedException("Google Mail labels are not supported on this server!");
            }

            return base.AddRange(labels);
        }

        public new bool Remove(string label)
        {
            if (!Client.Capabilities.XGMExt1)
            {
                throw new NotSupportedException("Google Mail labels are not supported on this server!");
            }

            return base.Remove(label);
        }

        public new bool RemoveRange(int index, int count)
        {
            if (!Client.Capabilities.XGMExt1)
            {
                throw new NotSupportedException("Google Mail labels are not supported on this server!");
            }

            return base.RemoveRange(index, count);
        }

        public new bool RemoveRange(IEnumerable<string> labels)
        {
            if (!Client.Capabilities.XGMExt1)
            {
                throw new NotSupportedException("Google Mail labels are not supported on this server!");
            }

            return base.RemoveRange(labels);
        }
    }
}