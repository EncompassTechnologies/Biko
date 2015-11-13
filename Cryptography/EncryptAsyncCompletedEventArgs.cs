using System;
using System.ComponentModel;

namespace Communications.Cryptography.OpenPGP
{
    public class EncryptAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public EncryptAsyncCompletedEventArgs(Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
        }
    }
}
