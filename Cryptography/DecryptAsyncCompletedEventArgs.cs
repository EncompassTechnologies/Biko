using System;
using System.ComponentModel;

namespace Communications.Cryptography.OpenPGP
{
    public class DecryptAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public DecryptAsyncCompletedEventArgs(Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
        }
    }
}
