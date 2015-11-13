using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpException : Exception
    {
        private FtpResponse _response = new FtpResponse();

        public FtpException()
        {
        }

        public FtpException(string message)
            : base(message)
        {
        }

        public FtpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FtpException(string message, FtpResponse response, Exception innerException)
            : base(message, innerException)
        {
            _response = response;
        }

        public FtpException(string message, FtpResponse response)
            : base(message)
        {
            _response = response;
        }

        protected FtpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpResponse LastResponse
        {
            get
            {
                return _response;
            }
        }

        public override string Message
        {
            get
            {
                if (_response.Code == FtpResponseCode.None)
                {
                    return base.Message;
                }
                else
                {
                    return String.Format("{0}  (Last Server Response: {1}  {2})", base.Message, _response.Text, _response.Code); ;
                }
            }
        }
    }
}
