using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpConnectionOpenException : FtpException
    {
        public FtpConnectionOpenException()
        {
        }

        public FtpConnectionOpenException(string message)
            : base(message)
        {
        }

        public FtpConnectionOpenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpConnectionOpenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpConnectionOpenException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpConnectionOpenException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
