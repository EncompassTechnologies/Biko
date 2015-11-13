using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpCommandResponseTimeoutException : FtpException
    {
        public FtpCommandResponseTimeoutException()
        {
        }

        public FtpCommandResponseTimeoutException(string message)
            : base(message)
        {
        }

        public FtpCommandResponseTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpCommandResponseTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpCommandResponseTimeoutException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpCommandResponseTimeoutException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
