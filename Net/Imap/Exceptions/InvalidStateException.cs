using System;

namespace Communications.Net.Imap.Exceptions
{
    public class InvalidStateException : Exception
    {
        public InvalidStateException()
        {
        }

        public InvalidStateException(string message)
            : base(message)
        {
        }
    }
}