using System;

namespace Communications.Net.Imap.Exceptions
{
    public class OperationFailedException : Exception
    {
        public OperationFailedException()
        {
        }

        public OperationFailedException(string message)
            : base(message)
        {
        }
    }
}