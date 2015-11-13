using Communications.Net.Imap.Constants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Communications.Net.Imap.Collections
{
    public class FolderFlagCollection : ImapObjectCollection<string>
    {
        private readonly Folder _folder;

        public FolderFlagCollection(ImapClient client, Folder folder)
            : base(client)
        {
            _folder = folder;
        }

        public FolderFlagCollection(IEnumerable<string> items, ImapClient client, Folder folder)
            : base(client, items)
        {
            _folder = folder;
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
            if (!Client.Capabilities.Metadata || _folder.AllowedPermanentFlags == null || !_folder.AllowedPermanentFlags.Intersect(flags).Any())
            {
                return false;
            }

            IList<string> data = new List<string>();

            if (Client.SendAndReceive(string.Format(ImapCommands.SetMetaData, _folder.Path, Client.Behavior.SpecialUseMetadataPath, string.Join(" ", _folder.Flags.Concat(flags.Where(_ => !string.IsNullOrEmpty(_))).Distinct().ToArray())), ref data))
            {
                AddRangeInternal(flags.Except(List));
                return true;
            }

            return false;
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
            if (!Client.Capabilities.Metadata || _folder.AllowedPermanentFlags == null || !_folder.AllowedPermanentFlags.Intersect(flags).Any())
            {
                return false;
            }

            IList<string> data = new List<string>();

            if (!Client.SendAndReceive(string.Format(ImapCommands.SetMetaData, _folder.Path, Client.Behavior.SpecialUseMetadataPath, string.Join(" ", _folder.Flags.Except(flags.Where(_ => !string.IsNullOrEmpty(_))).ToArray())), ref data))
            {
                return false;
            }

            foreach (var flag in flags)
            {
                List.Remove(flag);
            }

            return true;
        }
    }
}