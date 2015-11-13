using System;

namespace Communications.Net.Ftp
{
    public class PutFileUniqueAsyncCompletedEventArgs : EventArgs
    {
        private Exception _error;
        private bool _cancelled;

        public PutFileUniqueAsyncCompletedEventArgs(Exception error, bool cancelled)
        {
            _error = error;
            _cancelled = cancelled;
        }

        public Exception Error
        {
            get
            {
                return _error;
            }
        }

        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }
    }
}
