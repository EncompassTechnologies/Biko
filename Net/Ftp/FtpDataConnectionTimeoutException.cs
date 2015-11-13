using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpDataConnectionTimeoutException : FtpDataConnectionException
    {
        public FtpDataConnectionTimeoutException()
        {
        }

        public FtpDataConnectionTimeoutException(string message)
            : base(message)
        {
        }

        public FtpDataConnectionTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpDataConnectionTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpDataConnectionTimeoutException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpDataConnectionTimeoutException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
