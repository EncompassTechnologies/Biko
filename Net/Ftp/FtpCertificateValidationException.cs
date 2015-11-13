using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpCertificateValidationException : FtpSecureConnectionException
    {
        public FtpCertificateValidationException()
        {
        }

        public FtpCertificateValidationException(string message)
            : base(message)
        {
        }

        public FtpCertificateValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpCertificateValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpCertificateValidationException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpCertificateValidationException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
