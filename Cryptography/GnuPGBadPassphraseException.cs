using System;
using System.Runtime.Serialization;

namespace Communications.Cryptography.OpenPGP
{
    [Serializable()]
    public class GnuPGBadPassphraseException : Exception
    {
        public GnuPGBadPassphraseException()
        {
        }

        public GnuPGBadPassphraseException(string message)
            : base(message)
        {
        }

        public GnuPGBadPassphraseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GnuPGBadPassphraseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
