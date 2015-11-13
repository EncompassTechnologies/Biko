using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpDataTransferException : FtpException
    {
        public FtpDataTransferException()
        {
        }

        public FtpDataTransferException(string message)
            : base(message)
        {
        }

        public FtpDataTransferException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpDataTransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpDataTransferException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpDataTransferException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
