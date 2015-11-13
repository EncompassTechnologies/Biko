using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class PutFileAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public PutFileAsyncCompletedEventArgs(Exception error, bool canceled)
            : base(error, canceled, null)
        {
        }
    }
}
