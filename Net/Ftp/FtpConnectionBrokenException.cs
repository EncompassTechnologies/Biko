using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpConnectionBrokenException : FtpException
    {
        public FtpConnectionBrokenException()
        {
        }

        public FtpConnectionBrokenException(string message)
            : base(message)
        {
        }

        public FtpConnectionBrokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpConnectionBrokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpConnectionBrokenException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpConnectionBrokenException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
