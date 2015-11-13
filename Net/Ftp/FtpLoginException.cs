using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpLoginException : FtpException
    {
        public FtpLoginException()
        {
        }

        public FtpLoginException(string message)
            : base(message)
        {
        }

        public FtpLoginException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpLoginException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpLoginException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpLoginException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
