using System;

namespace Communications.Net.Ftp
{
    public class FtpTransferEventArgs : EventArgs
    {
        private long _bytesTransferred;

        public FtpTransferEventArgs(long bytesTransferred)
        {
            _bytesTransferred = bytesTransferred;
        }

        public long BytesTransferred
        {
            get
            {
                return _bytesTransferred;
            }
        }
    }
}
