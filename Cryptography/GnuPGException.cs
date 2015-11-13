using System;
using System.Runtime.Serialization;

namespace Communications.Cryptography.OpenPGP
{
    [Serializable()]
    public class GnuPGException : Exception
    {
        public GnuPGException()
        {
        }

        public GnuPGException(string message)
            : base(message)
        {
        }

        public GnuPGException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GnuPGException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
