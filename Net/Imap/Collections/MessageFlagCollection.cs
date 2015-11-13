using Communications.Net.Imap.Constants;
using Communications.Net.Imap.EncodingHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Communications.Net.Imap.Collections
{
    public class MessageFlagCollection : ImapObjectCollection<string>
    {
        private readonly Message _message;

        protected string AddType = "+FLAGS";
        protected string RemoveType = "-FLAGS";
        protected bool IsUTF7 = false;
        protected bool AddQuotes = false;

        public MessageFlagCollection()
        {
        }

        public MessageFlagCollection(Message message)
        {
            _message = message;
        }

        public MessageFlagCollection(ImapClient client, Message message)
            : base(client)
        {
            _message = message;
        }

        public bool Add(string flag)
        {
            if (string.IsNullOrEmpty(flag))
            {
                throw new ArgumentException("Flag cannot be empty");
            }

            return AddRange(new[] { flag });
        }

        public bool AddRange(IEnumerable<string> flags)
        {
            if (Client == null)
            {
                base.AddRangeInternal(flags);
                return true;
            }

            IList<string> data = new List<string>();

            if (!Client.SendAndReceive(string.Format(ImapCommands.Store, _message.UId, AddType, string.Join(" ", this.Concat(flags.Where(_ => !string.IsNullOrEmpty(_)))
                               .Where(_ => !_.Equals(Flags.MessageFlags.Recent))
                               .Distinct()
                               .Select(_ => (AddQuotes ? "\"" : "") + _ + (AddQuotes ? "\"" : ""))
                               .Select(_ => (IsUTF7 ? ImapUTF7.Encode(_) : _)).ToArray())), ref data))
            {
                return false;
            }

            AddRangeInternal(flags.Except(List));
            return true;
        }

        public bool Remove(string flag)
        {
            if (string.IsNullOrEmpty(flag))
            {
                throw new ArgumentException("Flag cannot be empty");
            }

            return RemoveRange(new[] { flag });
        }

        public bool RemoveRange(int index, int count)
        {
            return RemoveRange(List.Skip(index).Take(count));
        }

        public bool RemoveRange(IEnumerable<string> flags)
        {
            if (Client == null)
            {
                foreach (string flag in flags)
                {
                    RemoveInternal(flag);
                }

                return true;
            }

            if (Client.SelectedFolder != _message.Folder)
            {
                _message.Folder.Select();
            }

            IList<string> data = new List<string>();

            if (!Client.SendAndReceive(string.Format(ImapCommands.Store, _message.UId, RemoveType, string.Join(" ", flags.Where(_ => !string.IsNullOrEmpty(_))
                             .Select(_ => (AddQuotes ? "\"" : "") + _ + (AddQuotes ? "\"" : ""))
                             .Select(_ => IsUTF7 ? ImapUTF7.Encode(_) : _).ToArray())), ref data)) return false;
            {
                foreach (string flag in flags)
                {
                    List.Remove(flag);
                }
            }

            return true;
        }
    }
}