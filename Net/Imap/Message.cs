using Communications.Net.Imap.Collections;
using Communications.Net.Imap.Constants;
using Communications.Net.Imap.EncodingHelpers;
using Communications.Net.Imap.Enums;
using Communications.Net.Imap.Exceptions;
using Communications.Net.Imap.Extensions;
using Communications.Net.Imap.Flags;
using Communications.Net.Imap.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace Communications.Net.Imap
{
    public class Message : CommandProcessor
    {
        private readonly ImapClient _client;
        internal readonly Folder Folder;

        private MessageFetchMode _downloadProgress;
        private MessageFetchState _fetchState;
        private string _lastAddedHeader;

        internal Message()
        {
            Headers = new Dictionary<string, string>();
            BodyParts = new MessageContent[0];
            Attachments = new Attachment[0];
            EmbeddedResources = new Attachment[0];
            To = new List<MailAddress>();
            Cc = new List<MailAddress>();
            Bcc = new List<MailAddress>();
        }

        internal Message(ImapClient client, Folder folder)
            : this()
        {
            Flags = new MessageFlagCollection(client, this);
            Labels = new GMailMessageLabelCollection(client, this);
            _client = client;
            Folder = folder;
        }

        internal Message(long uId, ImapClient client, Folder folder)
            : this(client, folder)
        {
            UId = uId;
        }

        public long UId
        {
            get;
            private set;
        }

        public long SequenceNumber
        {
            get;
            set;
        }

        public long Size
        {
            get;
            private set;
        }

        public MessageFlagCollection Flags
        {
            get;
            private set;
        }

        public GMailMessageLabelCollection Labels
        {
            get;
            private set;
        }

        public DateTime? InternalDate
        {
            get;
            private set;
        }

        public DateTime? Date
        {
            get;
            set;
        }

        public Dictionary<string, string> Headers
        {
            get;
            set;
        }

        public string MimeVersion
        {
            get;
            set;
        }

        public string Subject
        {
            get;
            set;
        }

        public ContentType ContentType
        {
            get;
            set;
        }

        public string ContentTransferEncoding
        {
            get;
            set;
        }

        public MailAddress From
        {
            get;
            set;
        }

        public MailAddress Sender
        {
            get;
            set;
        }

        public List<MailAddress> To
        {
            get;
            set;
        }

        public List<MailAddress> Cc
        {
            get;
            set;
        }

        public List<MailAddress> Bcc
        {
            get;
            set;
        }

        public MailAddress ReturnPath
        {
            get;
            set;
        }

        public string Organization
        {
            get;
            set;
        }

        public MessageImportance Importance
        {
            get;
            set;
        }

        public MessageSensitivity Sensitivity
        {
            get;
            set;
        }

        public string MessageId
        {
            get;
            set;
        }

        public string Mailer
        {
            get;
            set;
        }

        public List<MailAddress> ReplyTo
        {
            get;
            set;
        }

        public string Language
        {
            get;
            set;
        }

        public string InReplyTo
        {
            get;
            set;
        }

        public string Comments
        {
            get;
            set;
        }

        public bool Seen
        {
            get
            {
                return Flags.Contains(MessageFlags.Seen);
            }
            set
            {
                if (value)
                    Flags.Add(MessageFlags.Seen);
                else
                    Flags.Remove(MessageFlags.Seen);
            }
        }

        public GMailMessageThread GmailThread
        {
            get;
            private set;
        }

        public MessageContent[] BodyParts
        {
            get;
            private set;
        }

        public Attachment[] Attachments
        {
            get;
            private set;
        }

        public Attachment[] EmbeddedResources
        {
            get;
            private set;
        }

        public MessageBody Body
        {
            get;
            set;
        }

        public long? GMailMessageId
        {
            get;
            private set;
        }

        public override void ProcessCommandResult(string data)
        {
            if (_client.Capabilities.XGMExt1 && !_downloadProgress.HasFlag(MessageFetchMode.GMailThreads))
            {
                TryProcessGmThreadId(data);
            }

            if (_client.Capabilities.XGMExt1 && !_downloadProgress.HasFlag(MessageFetchMode.GMailMessageId))
            {
                TryProcessGmMsgId(data);
            }

            if (!_downloadProgress.HasFlag(MessageFetchMode.Size))
            {
                TryProcessSize(data);
            }

            if (!_downloadProgress.HasFlag(MessageFetchMode.Flags))
            {
                TryProcessFlags(data);
            }

            if (!_downloadProgress.HasFlag(MessageFetchMode.GMailLabels))
            {
                TryProcessGmLabels(data);
            }

            if (!_downloadProgress.HasFlag(MessageFetchMode.InternalDate))
            {
                TryProcessInternalDate(data);
            }

            if (!_downloadProgress.HasFlag(MessageFetchMode.BodyStructure))
            {
                TryProcessBodyStructure(data);
            }

            if (Expressions.HeaderRex.IsMatch(data))
            {
                _fetchState = MessageFetchState.Headers;
                _downloadProgress = _downloadProgress | MessageFetchMode.Headers;
            }
            else if (string.IsNullOrEmpty(data))
            {
                _fetchState = MessageFetchState.None;
            }
            else if (_fetchState == MessageFetchState.Headers)
            {
                TryProcessHeader(data);
            }
        }

        private void TryProcessGmThreadId(string data)
        {
            Match threadMatch = Expressions.GMailThreadRex.Match(data);

            if (!threadMatch.Success)
            {
                return;
            }

            long threadId;

            if (!long.TryParse(threadMatch.Groups[1].Value, out threadId))
            {
                return;
            }

            GmailThread = Folder.GMailThreads.FirstOrDefault(_ => _.Id == threadId);

            if (GmailThread == null)
            {
                GmailThread = new GMailMessageThread(_client, Folder, threadId);
                Folder.GMailThreads.AddInternal(GmailThread);
            }

            GmailThread.Messages.AddInternal(this);
            _downloadProgress = _downloadProgress | MessageFetchMode.GMailThreads;
        }

        private void TryProcessGmMsgId(string data)
        {
            Match msgIdMatch = Expressions.GMailMessageIdRex.Match(data);

            if (!msgIdMatch.Success)
            {
                return;
            }

            long msgId;

            if (!long.TryParse(msgIdMatch.Groups[1].Value, out msgId))
            {
                return;
            }

            GMailMessageId = msgId;
            _downloadProgress = _downloadProgress | MessageFetchMode.GMailMessageId;
        }

        private void TryProcessSize(string data)
        {
            Match sizeMatch = Expressions.SizeRex.Match(data);

            if (!sizeMatch.Success)
            {
                return;
            }

            Size = long.Parse(sizeMatch.Groups[1].Value);
            _downloadProgress = _downloadProgress | MessageFetchMode.Size;
        }

        private void TryProcessFlags(string data)
        {
            Match flagsMatch = Expressions.FlagsRex.Match(data);

            if (!flagsMatch.Success)
            {
                return;
            }

            Flags.AddRangeInternal(flagsMatch.Groups[1].Value.Split(' ').Where(_ => !string.IsNullOrEmpty(_)));
            _downloadProgress = _downloadProgress | MessageFetchMode.Flags;
        }

        private void TryProcessGmLabels(string data)
        {
            // Fix by kirchik

            var match = Expressions.GMailLabelsRex.Match(data);

            if (match.Success)
            {
                _downloadProgress = _downloadProgress | MessageFetchMode.GMailLabels;
            }

            var labelsMatches = Expressions.GMailLabelSplitRex.Matches(match.Groups[1].Value);

            if (labelsMatches.Count == 0)
            {
                return;
            }

            foreach (Match labelsMatch in labelsMatches)
            {
                Labels.AddRangeInternal(labelsMatch.Groups.Cast<Group>()
                        .Skip(1)
                        .Select(_ => (_.Value.StartsWith("&") ? ImapUTF7.Decode(_.Value) : _.Value).Replace("\"", "")));
            }
        }

        private void TryProcessInternalDate(string data)
        {
            Match dateMatch = Expressions.InternalDateRex.Match(data);

            if (!dateMatch.Success)
            {
                return;
            }

            InternalDate = HeaderFieldParser.ParseDate(dateMatch.Groups[1].Value);
            _downloadProgress = _downloadProgress | MessageFetchMode.InternalDate;
        }

        internal void TryProcessHeader(string data)
        {
            Match headerMatch = Expressions.HeaderParseRex.Match(data);

            if (headerMatch.Success && !Headers.ContainsKey(headerMatch.Groups[1].Value.ToLower()))
            {
                _lastAddedHeader = headerMatch.Groups[1].Value.ToLower();
                Headers.Add(_lastAddedHeader, headerMatch.Groups[2].Value);
            }
            else if (headerMatch.Success)
            {
                _lastAddedHeader = headerMatch.Groups[1].Value.ToLower();
                Headers[_lastAddedHeader] = Headers[_lastAddedHeader] + _lastAddedHeader == MessageHeader.ContentType ? "; " : Environment.NewLine + headerMatch.Groups[2].Value;
            }
            else
            {
                Headers[_lastAddedHeader] = Headers[_lastAddedHeader] + data;
            }
        }

        private void BindHeadersToFields()
        {
            foreach (var header in Headers)
            {
                switch (header.Key)
                {
                    case MessageHeader.MimeVersion:
                        MimeVersion = header.Value;
                        break;

                    case MessageHeader.Sender:
                        Sender = HeaderFieldParser.ParseMailAddress(header.Value);
                        break;

                    case MessageHeader.Subject:
                        Subject = StringDecoder.Decode(header.Value, true);
                        break;

                    case MessageHeader.To:
                        if (To.Count == 0)
                        {
                            To = HeaderFieldParser.ParseMailAddressCollection(header.Value);
                        }
                        else
                        {
                            foreach (MailAddress addr in HeaderFieldParser.ParseMailAddressCollection(header.Value))
                                To.Add(addr);
                        }

                        break;

                    case MessageHeader.DeliveredTo:
                        To.Add(HeaderFieldParser.ParseMailAddress(header.Value));
                        break;

                    case MessageHeader.From:
                        From = HeaderFieldParser.ParseMailAddress(header.Value);
                        break;

                    case MessageHeader.Cc:
                        Cc = HeaderFieldParser.ParseMailAddressCollection(header.Value);
                        break;

                    case MessageHeader.Bcc:
                        Bcc = HeaderFieldParser.ParseMailAddressCollection(header.Value);
                        break;

                    case MessageHeader.Organisation:
                    case MessageHeader.Organization:
                        Organization = StringDecoder.Decode(header.Value, true);
                        break;

                    case MessageHeader.Date:
                        Date = HeaderFieldParser.ParseDate(header.Value);
                        break;

                    case MessageHeader.Importance:
                        Importance = header.Value.ToMessageImportance();
                        break;

                    case MessageHeader.ContentType:
                        ContentType = HeaderFieldParser.ParseContentType(header.Value);
                        break;

                    case MessageHeader.ContentTransferEncoding:
                        ContentTransferEncoding = header.Value;
                        break;

                    case MessageHeader.MessageId:
                        MessageId = header.Value;
                        break;

                    case MessageHeader.Mailer:
                    case MessageHeader.XMailer:
                        Mailer = header.Value;
                        break;

                    case MessageHeader.ReplyTo:
                        ReplyTo = HeaderFieldParser.ParseMailAddressCollection(header.Value);
                        break;

                    case MessageHeader.Sensitivity:
                        Sensitivity = header.Value.ToMessageSensitivity();
                        break;

                    case MessageHeader.ReturnPath:
                        ReturnPath = HeaderFieldParser.ParseMailAddress(header.Value.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                                    .Distinct()
                                    .FirstOrDefault());
                        break;

                    case MessageHeader.ContentLanguage:
                    case MessageHeader.Language:
                        Language = header.Value;
                        break;

                    case MessageHeader.InReplyTo:
                        InReplyTo = header.Value;
                        break;

                    case MessageHeader.Comments:
                        Comments = header.Value;
                        break;
                }
            }
        }

        private void TryProcessBodyStructure(string data)
        {
            Match bstructMatch = Expressions.BodyStructRex.Match(data);

            if (!bstructMatch.Success)
            {
                return;
            }

            using (var parser = new BodyStructureParser(bstructMatch.Groups[1].Value, _client, this))
            {
                BodyParts = parser.Parse();
            }

            Body = new MessageBody(_client,
                BodyParts.FirstOrDefault(
                    _ =>
                        _.ContentDisposition == null && _.ContentType != null &&
                        _.ContentType.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase)),
                BodyParts.FirstOrDefault(
                    _ =>
                        _.ContentDisposition == null && _.ContentType != null &&
                        _.ContentType.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase)));

            Attachments = (from part in BodyParts
                           where
                               part.ContentDisposition != null &&
                               part.ContentDisposition.DispositionType == DispositionTypeNames.Attachment
                           select new Attachment(part)).ToArray();

            EmbeddedResources = (from part in BodyParts
                                 where
                                     part.ContentDisposition != null &&
                                     (part.ContentDisposition.DispositionType == DispositionTypeNames.Inline || !string.IsNullOrEmpty(part.ContentId))
                                 select new Attachment(part)).ToArray();

            _downloadProgress = _downloadProgress | MessageFetchMode.BodyStructure;
        }

        public string DownloadRawMessage()
        {
            IList<string> data = new List<string>();

            if (!_client.SendAndReceive(string.Format(ImapCommands.Fetch, UId, "BODY.PEEK[]"), ref data))
            {
                throw new OperationFailedException("The raw message could not be downloaded");
            }

            var sb = new StringBuilder();

            for (var i = 1; i < data.Count; i++)
            {
                if ((data[i].StartsWith(")") || data[i].Contains("UID")) && (i == data.Count - 1 || i == data.Count - 2))
                {
                    break;
                }

                sb.AppendLine(data[i]);
            }

            return sb.ToString();
        }

        public bool Download(MessageFetchMode mode = MessageFetchMode.ClientDefault, bool reloadHeaders = false)
        {
            if (mode == MessageFetchMode.ClientDefault)
            {
                mode = _client.Behavior.MessageFetchMode;
            }

            if (mode == MessageFetchMode.None)
            {
                return true;
            }

            var fetchParts = new StringBuilder();

            if (mode.HasFlag(MessageFetchMode.Flags) && !_downloadProgress.HasFlag(MessageFetchMode.Flags))
            {
                fetchParts.Append("FLAGS ");
            }

            if (mode.HasFlag(MessageFetchMode.InternalDate) && !_downloadProgress.HasFlag(MessageFetchMode.InternalDate))
            {
                fetchParts.Append("INTERNALDATE ");
            }

            if (mode.HasFlag(MessageFetchMode.Size) && !_downloadProgress.HasFlag(MessageFetchMode.Size))
            {
                fetchParts.Append("RFC822.SIZE ");
            }

            if (mode.HasFlag(MessageFetchMode.Headers) && (!_downloadProgress.HasFlag(MessageFetchMode.Headers) || reloadHeaders))
            {
                Headers.Clear();

                if (_client.Behavior.RequestedHeaders == null || _client.Behavior.RequestedHeaders.Length == 0)
                {
                    fetchParts.Append("BODY.PEEK[HEADER] ");
                }
                else
                {
                    fetchParts.Append("BODY.PEEK[HEADER.FIELDS (" +
                                      string.Join(" ",
                                          _client.Behavior.RequestedHeaders.Where(_ => !string.IsNullOrEmpty(_))
                                              .Select(_ => _.ToUpper())
                                              .ToArray()) + ")] ");
                }
            }

            if (mode.HasFlag(MessageFetchMode.BodyStructure) && !_downloadProgress.HasFlag(MessageFetchMode.BodyStructure))
            {
                fetchParts.Append("BODYSTRUCTURE ");
            }

            if (_client.Capabilities.XGMExt1)
            {
                if (mode.HasFlag(MessageFetchMode.GMailMessageId) && !_downloadProgress.HasFlag(MessageFetchMode.GMailMessageId))
                {
                    fetchParts.Append("X-GM-MSGID ");
                }

                if (mode.HasFlag(MessageFetchMode.GMailThreads) && !_downloadProgress.HasFlag(MessageFetchMode.GMailThreads))
                {
                    fetchParts.Append("X-GM-THRID ");
                }

                if (mode.HasFlag(MessageFetchMode.GMailLabels) && !_downloadProgress.HasFlag(MessageFetchMode.GMailLabels))
                {
                    fetchParts.Append("X-GM-LABELS ");
                }
            }

            IList<string> data = new List<string>();
            if (fetchParts.Length > 0 && !_client.SendAndReceive(string.Format(ImapCommands.Fetch, UId, fetchParts.ToString().Trim()), ref data))
            {
                return false;
            }
            else
            {
                NormalizeAndProcessFetchResult(data);
            }

            BindHeadersToFields();

            if (!mode.HasFlag(MessageFetchMode.Body) || BodyParts == null)
            {
                return true;
            }

            foreach (MessageContent bodyPart in BodyParts)
            {
                if (mode.HasFlag(MessageFetchMode.Full) || (bodyPart.ContentDisposition == null && bodyPart.ContentType != null && (bodyPart.ContentType.MediaType == "text/plain" || bodyPart.ContentType.MediaType == "text/html")))
                {
                    bodyPart.Download();
                }
            }

            return true;
        }

        private void NormalizeAndProcessFetchResult(IList<string> data)
        {
            var buffer = "";

            for (var i = 0; i < data.Count; i++)
            {
                var str = string.IsNullOrEmpty(buffer) ? data[i] : buffer + data[i];

                if ((str.Split(')').Length + (i == 0 ? 1 : 0)) >= (str.Split('(').Length) || (i + 1 < data.Count && Regex.IsMatch(data[i + 1], @"^[A-Za-z-]+:")))
                {
                    ProcessCommandResult(str);
                    buffer = "";
                }
                else if (i + 1 < data.Count)
                {
                    buffer += data[i];
                }
            }
        }

        public override byte[] AppendCommandData(string serverResponse)
        {
            return base.AppendCommandData(serverResponse);
        }

        public bool MoveTo(Folder folder, bool downloadCopy = false)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            return CopyTo(folder, downloadCopy) && Remove();
        }

        public bool CopyTo(Folder folder, bool downloadCopy = false)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            IList<string> data = new List<string>();
            if (!_client.SendAndReceive(string.Format(ImapCommands.Copy, UId, folder.Path), ref data, this))
            {
                return false;
            }

            var m = Expressions.CopyUIdRex.Match(data.FirstOrDefault() ?? "");

            if (m.Success)
            {
                folder.Search("UID " + m.Groups[3].Value);
            }

            return true;
        }

        public bool Remove()
        {
            if (!Flags.Add(MessageFlags.Deleted) || !Folder.Expunge()) return false;
            Folder.Messages.RemoveInternal(this);
            return true;
        }

        public static Message FromEml(string eml)
        {
            return Communications.Net.Imap.MessageBuilder.FromEml(eml);
        }

        public string ToEml()
        {
            return Communications.Net.Imap.MessageBuilder.ToEml(this);
        }

        public void SaveTo(string folderPath, string fileName)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException("Path cannot be null or empty", "path");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty", "fileName");
            }

            Save(Path.Combine(folderPath, fileName));
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", "path");
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    textWriter.Write(ToEml());
                }
            }
        }
    }
}