using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class GetFileAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public GetFileAsyncCompletedEventArgs(Exception error, bool canceled)
            : base(error, canceled, null)
        {
        }
    }
}
