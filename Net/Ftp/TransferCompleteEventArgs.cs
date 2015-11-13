using System;

namespace Communications.Net.Ftp
{
    public class TransferCompleteEventArgs : EventArgs
    {
        private long _bytesTransferred;
        private int _bytesPerSecond;
        private TimeSpan _elapsedTime;

        public TransferCompleteEventArgs(long bytesTransferred, int bytesPerSecond, TimeSpan elapsedTime)
        {
            _bytesTransferred = bytesTransferred;
            _bytesPerSecond = bytesPerSecond;
            _elapsedTime = elapsedTime;
        }

        public long BytesTransferred
        {
            get
            {
                return _bytesTransferred;
            }
        }

        public int BytesPerSecond
        {
            get
            {
                return _bytesPerSecond;
            }
        }

        public int KilobytesPerSecond
        {
            get
            {
                return _bytesPerSecond / 1024;
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return _elapsedTime;
            }
        }
    }
}
