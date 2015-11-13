using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpConnectionClosedException : FtpException
    {
        public FtpConnectionClosedException()
        {
        }

        public FtpConnectionClosedException(string message)
            : base(message)
        {
        }

        public FtpConnectionClosedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpConnectionClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpConnectionClosedException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpConnectionClosedException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
