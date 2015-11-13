using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpFileIntegrityException : FtpException
    {
        public FtpFileIntegrityException()
        {
        }

        public FtpFileIntegrityException(string message)
            : base(message)
        {
        }

        public FtpFileIntegrityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpFileIntegrityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpFileIntegrityException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpFileIntegrityException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
