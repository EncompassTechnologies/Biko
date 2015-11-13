using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Communications.Cryptography.OpenPGP
{
    public class GnuPGKey
    {
        private string _key;
        private DateTime _keyExpiration;
        private string _userId;
        private string _userName;
        private string _subKey;
        private DateTime _subKeyExpiration;
        private string _raw;

        public GnuPGKey(string raw)
        {
            _raw = raw;
            ParseRaw();
        }

        public string Key
        {
            get
            {
                return _key;
            }
        }

        public DateTime KeyExpiration
        {
            get
            {
                return _keyExpiration;
            }
        }

        public string UserId
        {
            get
            {
                return _userId;
            }
        }

        public string UserName
        {
            get
            {
                return _userName;
            }
        }

        public string SubKey
        {
            get
            {
                return _subKey;
            }
        }

        public DateTime SubKeyExpiration
        {
            get
            {
                return _subKeyExpiration;
            }
        }

        public string Raw
        {
            get
            {
                return _raw;
            }
        }

        private void ParseRaw()
        {
            string[] lines = _raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] pub = SplitSpaces(lines[0]);
            string uid = lines[1];
            string[] sub = SplitSpaces(lines[2]);

            _key = pub[1];
            _keyExpiration = DateTime.Parse(pub[2]);
            _subKey = sub[1];
            _subKeyExpiration = DateTime.Parse(sub[2]);

            ParseUid(uid);
        }

        private string[] SplitSpaces(string input)
        {
            char[] splitChar = { ' ' };
            return input.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
        }

        private void ParseUid(string uid)
        {
            Regex name = new Regex(@"(?<=uid).*(?=<)");
            Regex userId = new Regex(@"(?<=<).*(?=>)");

            _userName = name.Match(uid).ToString().Trim();
            _userId = userId.Match(uid).ToString();
        }
    }
}
