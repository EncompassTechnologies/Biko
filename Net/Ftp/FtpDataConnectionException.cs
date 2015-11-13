using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpDataConnectionException : FtpConnectionClosedException
    {
        public FtpDataConnectionException()
        {
        }

        public FtpDataConnectionException(string message)
            : base(message)
        {
        }

        public FtpDataConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpDataConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpDataConnectionException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpDataConnectionException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
