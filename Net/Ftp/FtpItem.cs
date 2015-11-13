using System;
using System.IO;
using System.Text;

namespace Communications.Net.Ftp
{
    public enum FtpItemType
    {
        Directory,
        File,
        SymbolicLink,
        BlockSpecialFile,
        CharacterSpecialFile,
        NamedSocket,
        DomainSocket,
        Unknown
    }

    public class FtpItem
    {
        private string _name;
        private DateTime _modified;
        private long _size;
        private string _symbolicLink;
        private FtpItemType _itemType;
        private string _attributes;
        private string _rawText;
        private string _parentPath;

        public FtpItem(string name, DateTime modified, long size, string symbolicLink, string attributes, FtpItemType itemType, string rawText)
        {
            _name = name;
            _modified = modified;
            _size = size;
            _symbolicLink = symbolicLink;
            _attributes = attributes;
            _itemType = itemType;
            _rawText = rawText;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string Attributes
        {
            get
            {
                return _attributes;
            }
        }

        public DateTime Modified
        {
            get
            {
                return _modified;
            }
        }

        public long Size
        {
            get
            {
                return _size;
            }
        }

        public string SymbolicLink
        {
            get
            {
                return _symbolicLink;
            }
        }

        public FtpItemType ItemType
        {
            get
            {
                return _itemType;
            }
        }

        public string RawText
        {
            get
            {
                return _rawText;
            }
        }

        public string ParentPath
        {
            get
            {
                return _parentPath;
            }

            set
            {
                _parentPath = value;
            }
        }

        public string FullPath
        {
            get
            {
                return _parentPath == "/" || _parentPath == "//" ? String.Format("{0}{1}", _parentPath, _name) : String.Format("{0}/{1}", _parentPath, _name);
            }
        }
    }
}
