using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpSecureConnectionException : FtpException
    {
        public FtpSecureConnectionException()
        {
        }

        public FtpSecureConnectionException(string message)
            : base(message)
        {
        }

        public FtpSecureConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpSecureConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpSecureConnectionException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpSecureConnectionException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
