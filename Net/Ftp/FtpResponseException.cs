using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpResponseException : FtpException
    {
        public FtpResponseException()
        {
        }

        public FtpResponseException(string message, FtpResponse response)
            : base(message, response)
        {
        }

        public FtpResponseException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        protected FtpResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
