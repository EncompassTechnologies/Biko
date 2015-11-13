using Communications.Net.Imap.Collections;
using Communications.Net.Imap.Constants;
using Communications.Net.Imap.EncodingHelpers;
using Communications.Net.Imap.Enums;
using Communications.Net.Imap.Exceptions;
using Communications.Net.Imap.Extensions;
using Communications.Net.Imap.Flags;
using Communications.Net.Imap.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Communications.Net.Imap
{
    public class Folder
    {
        private readonly ImapClient _client;
        private readonly Folder _parent;
        private FolderFlagCollection _flags;
        private MessageCollection _messages;
        private string _name;
        private string _path;
        private FolderCollection _subFolders;

        internal Folder()
        { }

        internal Folder(string path, IEnumerable<string> flags, ref Folder parent, ImapClient client)
        {
            _path = path;
            _name = ImapUTF7.Decode(_path.Split(client.Behavior.FolderDelimeter).Last());
            UpdateFlags(flags);
            _parent = parent;
            _client = client;
            GMailThreads = new GMailThreadCollection();
        }

        internal ImapClient Client
        {
            get
            {
                return _client;
            }
        }

        public long Exists
        {
            get;
            private set;
        }

        public long Recent
        {
            get;
            private set;
        }

        public long FirstUnseen
        {
            get;
            private set;
        }

        public long Unseen
        {
            get;
            private set;
        }

        public FolderCollection SubFolders
        {
            get
            {
                return _subFolders ?? (_subFolders = HasChildren ? _client.GetFolders(_path + _client.Behavior.FolderDelimeter, _client.Folders, this) : new FolderCollection(_client, this));
            }
            internal set
            {
                _subFolders = value;
            }
        }

        public MessageCollection Messages
        {
            get
            {
                if (_messages != null)
                {
                    return _messages;
                }

                _messages = new MessageCollection(_client, this);

                if (_client.Behavior.AutoPopulateFolderMessages)
                {
                    _messages.Download();
                }

                return _messages;
            }
            internal set
            {
                _messages = value;
            }
        }

        public GMailThreadCollection GMailThreads
        {
            get;
            internal set;
        }

        public bool HasChildren
        {
            get;
            internal set;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (!Rename(value))
                {
                    throw new OperationFailedException("Failed to rename folder");
                }
            }
        }

        public bool Selectable
        {
            get;
            private set;
        }

        public string Path
        {
            get
            {
                return _path;
            }
            internal set
            {
                _path = value;
            }
        }

        public FolderFlagCollection Flags
        {
            get
            {
                return _flags;
            }
        }

        public IEnumerable<string> AllowedPermanentFlags
        {
            get;
            internal set;
        }

        public long UidNext
        {
            get;
            private set;
        }

        public string UidValidity
        {
            get;
            private set;
        }

        internal static Folder Parse(string commandResult, ref Folder parent, ImapClient client)
        {
            Match match = Expressions.FolderParseRex.Match(commandResult);

            if (match.Success && match.Groups.Count == 4)
            {
                string[] flags = match.Groups[1].Value.Split(' ');
                string path = match.Groups[3].Value;

                if (client.Behavior.FolderDelimeter == '\0')
                {
                    client.Behavior.FolderDelimeter = string.IsNullOrEmpty(match.Groups[2].Value) ? '"' : match.Groups[2].Value.ToCharArray()[0];
                }

                return new Folder(path, flags, ref parent, client);
            }

            return null;
        }

        internal void UpdateFlags(string flags)
        {
            UpdateFlags(flags.Split(' '));
        }

        internal void UpdateFlags(IEnumerable<string> flags)
        {
            _flags = new FolderFlagCollection((flags ?? new string[0]).Where(_ => !string.IsNullOrEmpty(_)), _client, this);
            Selectable = !flags.Contains(FolderFlags.NoSelect);
            HasChildren = flags.Contains(FolderFlags.HasChildren);
        }

        internal void UpdatePath(string parentPath)
        {
            int i = _path.LastIndexOf(_client.Behavior.FolderDelimeter);
            _path = parentPath + _path.Substring(i, _path.Length - i);
        }

        internal bool Rename(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Folder name cannot be empty");
            }

            IList<string> data = new List<string>();
            string encodedName = ImapUTF7.Encode(name);
            int i = _path.LastIndexOf(_client.Behavior.FolderDelimeter);
            string newPath = i < 1 ? encodedName : _path.Substring(0, i + 1) + encodedName;

            if (!_client.SendAndReceive(string.Format(ImapCommands.Rename, _path, newPath), ref data))
            {
                return false;
            }

            _name = name;
            _path = newPath;

            if (!HasChildren || _subFolders == null)
            {
                return true;
            }

            foreach (var folder in SubFolders)
            {
                folder.UpdatePath(_path);
            }

            return true;
        }

        public bool Remove()
        {
            if (!Selectable)
            {
                throw new InvalidOperationException("A non-selectable folder cannot be deleted. This error may occur if the folder has subfolders.");
            }

            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Delete, _path), ref data))
            {
                return false;
            }

            if (_parent != null)
            {
                _parent._subFolders.RemoveInternal(this);
            }
            else
            {
                _client.Folders.RemoveInternal(this);
            }

            return true;
        }

        public bool Expunge()
        {
            Folder selectedFolder = _client.SelectedFolder;

            if (selectedFolder != null && !Equals(selectedFolder))
            {
                Select();
            }

            IList<string> data = new List<string>();
            bool result = _client.SendAndReceive(ImapCommands.Expunge, ref data);

            if (selectedFolder != null && !Equals(selectedFolder))
            {
                selectedFolder.Select();
            }

            return result;
        }

        private void ProcessSelectOrExamineResult(IList<string> data)
        {
            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.Count - 1; i++)
            {
                Match m = Expressions.ExistsRex.Match(data[i]);

                if (m.Success)
                {
                    Exists = long.Parse(m.Groups[1].Value);
                    continue;
                }

                m = Expressions.RecentRex.Match(data[i]);

                if (m.Success)
                {
                    Recent = long.Parse(m.Groups[1].Value);
                    continue;
                }

                m = Expressions.UnseenRex.Match(data[i]);

                if (m.Success)
                {
                    FirstUnseen = long.Parse(m.Groups[1].Value);
                    continue;
                }

                m = Expressions.UIdValidityRex.Match(data[i]);

                if (m.Success)
                {
                    UidValidity = m.Groups[1].Value;
                    continue;
                }

                m = Expressions.UIdNextRex.Match(data[i]);

                if (m.Success)
                {
                    UidNext = long.Parse(m.Groups[1].Value);
                    continue;
                }

                m = Expressions.PermanentFlagsRex.Match(data[i]);

                if (m.Success)
                {
                    AllowedPermanentFlags = m.Groups[1].Value.Split(' ').Where(_ => !string.IsNullOrEmpty(_));
                }
            }
        }

        public bool Examine()
        {
            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Examine, _path), ref data))
            {
                return false;
            }

            ProcessSelectOrExamineResult(data);
            return true;
        }

        public bool Status(string[] statusFields)
        {
            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Status, _path, string.Join(" ", statusFields)), ref data))
            {
                return false;
            }

            var result = data.FirstOrDefault(_ => _.ToUpper().StartsWith("* STATUS"));

            if (result == null)
            {
                return false;
            }

            var matches = Expressions.StatusRex.Matches(result);

            foreach (Match m in matches)
            {
                var name = m.Groups[1].Value.ToUpper();
                var value = m.Groups[2].Value;

                switch (name)
                {
                    case FolderStatusFields.Messages:
                        Exists = long.Parse(value);
                        break;

                    case FolderStatusFields.Recent:
                        Recent = long.Parse(value);
                        break;

                    case FolderStatusFields.UIdNext:
                        UidNext = long.Parse(value);
                        break;

                    case FolderStatusFields.UIdValidity:
                        UidValidity = value;
                        break;

                    case FolderStatusFields.Unseen:
                        Unseen = long.Parse(value);
                        break;
                }
            }

            return true;
        }

        public bool Select()
        {
            if (!Selectable)
            {
                throw new InvalidOperationException("A non-selectable folder cannot be selected.");
            }

            if (_client.SelectedFolder == this)
            {
                return true;
            }

            if (_client.IdleState == IdleState.On)
            {
                _client.StopIdling();
            }

            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Select, _path), ref data))
            {
                return false;
            }

            ProcessSelectOrExamineResult(data);
            _client.SelectedFolder = this;
            return true;
        }

        public bool StartIdling()
        {
            return _client.Capabilities.Idle && Select() && _client.StartIdling();
        }

        public void StopIdling()
        {
            _client.StopIdling();
        }

        internal Message[] Fetch(IEnumerable<long> uIds, MessageFetchMode mode = MessageFetchMode.ClientDefault)
        {
            if (_client.SelectedFolder != this && !Select())
            {
                throw new OperationFailedException("The folder couldn't be selected for search.");
            }

            var result = new List<Message>();

            foreach (Message msg in uIds.Select(uId => Messages.FirstOrDefault(_ => _.UId == uId) ?? new Message(uId, _client, this)))
            {
                msg.Download(mode);

                if (!Messages.Contains(msg))
                {
                    Messages.AddInternal(msg);
                }

                result.Add(msg);
            }

            return result.ToArray();
        }

        internal long[] SearchMessageIds(string query = "ALL", int count = -1)
        {
            if (_client.SelectedFolder != this && !Select())
            {
                throw new OperationFailedException("The folder couldn't be selected for search.");
            }

            if (query.ToUpper() == "ALL" && _client.Behavior.SearchAllNotSupported)
            {
                query = "SINCE 0000-00-00";
            }

            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Search, query), ref data))
            {
                throw new ArgumentException("The search query couldn't be processed");
            }

            var result = Expressions.SearchRex.Match(data.FirstOrDefault(Expressions.SearchRex.IsMatch) ?? "");

            if (!result.Success)
            {
                return new long[0];
            }

            return count < 0 ? result.Groups[1].Value.Trim().Split(' ').Select(long.Parse).ToArray() : result.Groups[1].Value.Trim().Split(' ').OrderByDescending(_ => _).Take(count).Select(long.Parse).ToArray();
        }

        public Message[] Search(string query = "ALL", MessageFetchMode mode = MessageFetchMode.ClientDefault, int count = -1)
        {
            return Fetch(SearchMessageIds(query, count), mode);
        }

        public Message[] Search(long[] uIds, MessageFetchMode mode = MessageFetchMode.ClientDefault)
        {
            return Fetch(uIds, mode);
        }

        public bool AppendMessage(string eml, IEnumerable<string> flags = null, DateTime? date = null)
        {
            if (string.IsNullOrEmpty(eml))
            {
                throw new ArgumentException("eml cannot be empty");
            }

            IList<string> data = new List<string>();
            var msgUploader = new MessageUploader(eml);
            var dateStr = date.HasValue ? date.Value.ToString("dd-MM-yyyy") : "";

            if (dateStr.StartsWith("0"))
            {
                dateStr = " " + dateStr.Substring(1, dateStr.Length - 1);
            }

            return _client.SendAndReceive(string.Format(ImapCommands.Append, _path) + " {" + eml.Length + "}", ref data, msgUploader);
        }

        public bool AppendMessage(System.Net.Mail.MailMessage mailMessage)
        {
            if (mailMessage == null)
            {
                throw new ArgumentNullException("mailMessage", "mailMessage cannot be null");
            }

            try
            {
                return AppendMessage(mailMessage.ToEml());
            }
            catch
            {
                return false;
            }
        }

        public bool EmptyFolder()
        {
            IEnumerable<string> ids = SearchMessageIds().GroupUIdSequences();

            if (!ids.Any())
            {
                return true;
            }

            foreach (string group in ids)
            {
                IList<string> data = new List<string>();
                if (!_client.SendAndReceive(string.Format(ImapCommands.Store, group, "+FLAGS", MessageFlags.Deleted), ref data))
                {
                    return false;
                }
            }

            if (!Expunge())
            {
                return false;
            }

            Messages.ClearInternal();
            Examine();
            return true;
        }

        public event EventHandler<IdleEventArgs> OnNewMessagesArrived;

        internal void RaiseNewMessagesArrived(IdleEventArgs args)
        {
            if (OnNewMessagesArrived == null)
            {
                return;
            }

            OnNewMessagesArrived(this, args);
        }
    }
}