using System;

namespace Communications.Net.Imap.Exceptions
{
    public class ServerAlertException : Exception
    {
        public ServerAlertException()
        {
        }

        public ServerAlertException(string message)
            : base(message)
        {
        }
    }
}