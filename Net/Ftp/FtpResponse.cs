using System;

namespace Communications.Net.Ftp
{
    public class FtpResponse
    {
        private string _rawText;
        private string _text;
        private FtpResponseCode _code = FtpResponseCode.None;
        private bool _isInformational;

        public FtpResponse()
        {
        }

        public FtpResponse(string rawText)
        {
            _rawText = rawText;
            _text = ParseText(rawText);
            _code = ParseCode(rawText);
            _isInformational = ParseInformational(rawText);
        }

        public FtpResponse(FtpResponse response)
        {
            _rawText = response.RawText;
            _text = response.Text;
            _code = response.Code;
            _isInformational = response.IsInformational;
        }

        public string RawText
        {
            get
            {
                return _rawText;
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        public FtpResponseCode Code
        {
            get
            {
                return _code;
            }
        }

        internal bool IsInformational
        {
            get
            {
                return _isInformational;
            }
        }

        private FtpResponseCode ParseCode(string rawText)
        {
            FtpResponseCode code = FtpResponseCode.None;

            if (rawText.Length >= 3)
            {
                string codeString = rawText.Substring(0, 3);
                int codeInt = 0;

                if (Int32.TryParse(codeString, out codeInt))
                {
                    code = (FtpResponseCode)codeInt;
                }
            }

            return code;
        }

        private string ParseText(string rawText)
        {
            if (rawText.Length > 4)
            {
                return rawText.Substring(4).Trim();
            }
            else
            {
                return string.Empty;
            }
        }

        private bool ParseInformational(string rawText)
        {
            if (rawText.Length >= 4 && rawText[3] == '-')
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
