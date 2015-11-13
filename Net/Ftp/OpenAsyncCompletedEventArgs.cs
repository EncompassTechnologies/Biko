using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class OpenAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public OpenAsyncCompletedEventArgs(Exception error, bool canceled)
            : base(error, canceled, null)
        {
        }
    }
}
