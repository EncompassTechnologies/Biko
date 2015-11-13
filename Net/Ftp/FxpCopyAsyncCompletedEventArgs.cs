using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class FxpCopyAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public FxpCopyAsyncCompletedEventArgs(Exception error, bool canceled)
            : base(error, canceled, null)
        {
        }
    }
}
