using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpAuthenticationException : FtpException
    {
        public FtpAuthenticationException()
        {
        }

        public FtpAuthenticationException(string message)
            : base(message)
        {
        }

        public FtpAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpAuthenticationException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpAuthenticationException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
