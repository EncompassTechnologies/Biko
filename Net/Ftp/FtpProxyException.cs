using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpProxyException : FtpException
    {
        public FtpProxyException()
        {
        }

        public FtpProxyException(string message)
            : base(message)
        {
        }

        public FtpProxyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpProxyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpProxyException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpProxyException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
