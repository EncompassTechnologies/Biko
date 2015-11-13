using System;

namespace Communications.Net.Ftp
{
    public class TransferProgressEventArgs : EventArgs
    {
        private int _bytesTransferred;
        private int _bytesPerSecond;
        private TimeSpan _elapsedTime;

        public TransferProgressEventArgs(int bytesTransferred, int bytesPerSecond, TimeSpan elapsedTime)
        {
            _bytesTransferred = bytesTransferred;
            _bytesPerSecond = bytesPerSecond;
            _elapsedTime = elapsedTime;
        }

        public int BytesTransferred
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
