using Communications.Net.Imap.Constants;
using Communications.Net.Imap.EncodingHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Communications.Net.Imap.Collections
{
    public class FolderCollection : ImapObjectCollection<Folder>
    {
        private Folder _parentFolder;

        public FolderCollection(ImapClient client, Folder parentFolder = null)
            : base(client)
        {
            _parentFolder = parentFolder;
        }

        public FolderCollection(IEnumerable<Folder> items, ImapClient client, Folder parentFolder = null)
            : base(client, items)
        {
            _parentFolder = parentFolder;
        }

        public Folder this[string name]
        {
            get
            {
                var result = List.FirstOrDefault(_ => _.Name.Equals(name));
                return result;
            }
        }

        public Folder Add(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentException("The folder name cannot be empty");
            }

            folderName = ImapUTF7.Encode(folderName);
            var path = _parentFolder == null ? folderName : _parentFolder.Path + Client.Behavior.FolderDelimeter + folderName;
            IList<string> data = new List<string>();

            if (!Client.SendAndReceive(string.Format(ImapCommands.Create, path), ref data))
            {
                return null;
            }

            var folder = new Folder(path, new string[0], ref _parentFolder, Client);

            if (Client.Behavior.ExamineFolders)
            {
                folder.Examine();
            }

            AddInternal(folder);
            return folder;
        }

        public bool Remove(Folder item)
        {
            return item.Remove();
        }

        public bool RemoveAt(int index)
        {
            return Remove(List[index]);
        }
    }
}