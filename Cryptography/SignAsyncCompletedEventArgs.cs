using System;
using System.ComponentModel;

namespace Communications.Cryptography.OpenPGP
{
    public class SignAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public SignAsyncCompletedEventArgs(Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
        }
    }
}
