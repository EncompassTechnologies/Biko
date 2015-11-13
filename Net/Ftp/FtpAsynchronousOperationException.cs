using System;
using System.Runtime.Serialization;

namespace Communications.Net.Ftp
{
    [Serializable()]
    public class FtpAsynchronousOperationException : FtpException
    {
        public FtpAsynchronousOperationException()
        {
        }

        public FtpAsynchronousOperationException(string message)
            : base(message)
        {
        }

        public FtpAsynchronousOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FtpAsynchronousOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FtpAsynchronousOperationException(string message, FtpResponse response, Exception innerException)
            : base(message, response, innerException)
        {
        }

        public FtpAsynchronousOperationException(string message, FtpResponse response)
            : base(message, response)
        {
        }
    }
}
