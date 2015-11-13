using System.Linq;

namespace Communications.Net.Imap
{
    public class Capability
    {
        public Capability(string commandResult)
        {
            Update(commandResult);
        }

        public bool Acl
        {
            get;
            private set;
        }

        public string[] All
        {
            get;
            private set;
        }

        public string[] AuthenticationMechanisms
        {
            get;
            private set;
        }

        public bool Binary
        {
            get;
            private set;
        }

        public bool Catenate
        {
            get;
            private set;
        }

        public bool Children
        {
            get;
            private set;
        }

        public string[] CompressionMechanisms
        {
            get;
            private set;
        }

        public bool CondStore
        {
            get;
            private set;
        }

        public string[] Contexts
        {
            get;
            private set;
        }

        public bool Convert
        {
            get;
            private set;
        }

        public bool CreateSpecialUse
        {
            get;
            private set;
        }

        public bool Enable
        {
            get;
            private set;
        }

        public bool ESearch
        {
            get;
            private set;
        }

        public bool ESort
        {
            get;
            private set;
        }

        public bool Filters
        {
            get;
            private set;
        }

        public bool Id
        {
            get;
            private set;
        }

        public bool Idle
        {
            get;
            private set;
        }

        public bool LoginDisabled
        {
            get;
            private set;
        }

        public bool Metadata
        {
            get;
            private set;
        }

        public bool Namespace
        {
            get;
            private set;
        }

        public bool XoAuth
        {
            get;
            private set;
        }

        public bool XoAuth2
        {
            get;
            private set;
        }

        public bool Quota
        {
            get;
            private set;
        }

        public bool Unselect
        {
            get;
            private set;
        }

        public bool XList
        {
            get;
            private set;
        }

        public bool XGMExt1
        {
            get;
            private set;
        }

        internal void Update(string commandResult)
        {
            if (string.IsNullOrEmpty(commandResult))
            {
                return;
            }

            commandResult = commandResult.Replace("* CAPABILITY IMAP4rev1 ", "");
            All = (All ?? new string[0]).Concat(commandResult.Split(' ').Where(_ => !string.IsNullOrEmpty(_.Trim()))).Distinct().ToArray();

            AuthenticationMechanisms = (AuthenticationMechanisms ?? new string[0]).Concat(All.Where(_ => _.StartsWith("AUTH="))
                .Select(_ => _.Substring(5, _.Length - 5))).Distinct().ToArray();

            CompressionMechanisms = (CompressionMechanisms ?? new string[0]).Concat(All.Where(_ => _.StartsWith("COMPRESS="))
                .Select(_ => _.Substring(9, _.Length - 9))).Distinct().ToArray();

            Contexts = (Contexts ?? new string[0]).Concat(All.Where(_ => _.StartsWith("CONTEXT="))
                .Select(_ => _.Substring(8, _.Length - 8))).Distinct().ToArray();

            foreach (string s in All)
            {
                switch (s)
                {
                    case "X-GM-EXT-1":
                        XGMExt1 = true;
                        break;

                    case "XLIST":
                        XList = true;
                        break;

                    case "UNSELECT":
                        Unselect = true;
                        break;

                    case "QUOTA":
                        Quota = true;
                        break;

                    case "AUTH=XOAUTH2":
                        XoAuth2 = true;
                        break;

                    case "AUTH=XOAUTH":
                        XoAuth = true;
                        break;

                    case "NAMESPACE":
                        Namespace = true;
                        break;

                    case "METADATA":
                        Metadata = true;
                        break;

                    case "LOGINDISABLED":
                        LoginDisabled = true;
                        break;

                    case "IDLE":
                        Idle = true;
                        break;

                    case "ID":
                        Id = true;
                        break;

                    case "FILTERS":
                        Filters = true;
                        break;

                    case "ESORT":
                        ESort = true;
                        break;

                    case "ESEARCH":
                        ESearch = true;
                        break;

                    case "ENABLE":
                        Enable = true;
                        break;

                    case "CREATE-SPECIAL-USE":
                        CreateSpecialUse = true;
                        break;

                    case "CONVERT":
                        Convert = true;
                        break;

                    case "CONDSTORE":
                        CondStore = true;
                        break;

                    case "CHILDREN":
                        Children = true;
                        break;

                    case "CATENATE":
                        Catenate = true;
                        break;

                    case "BINARY":
                        Binary = true;
                        break;

                    case "ACL":
                        Acl = true;
                        break;
                }
            }
        }
    }
}