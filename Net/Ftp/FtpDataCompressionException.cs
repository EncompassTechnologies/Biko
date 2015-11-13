using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpDataCompressionException : FtpException
    {
        public FtpDataCompressionException()
        {
        }

        public FtpDataCompressionException(string message)
            : base(message)
        {
        }

        public FtpDataCompressionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpDataCompressionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpDataCompressionException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpDataCompressionException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
