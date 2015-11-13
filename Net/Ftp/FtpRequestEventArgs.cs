using System;

namespace Communications.Net.Ftp
{
    public class FtpRequestEventArgs : EventArgs
    {
        private FtpRequest _request;

        public FtpRequestEventArgs(FtpRequest request)
        {
            _request = request;
        }

        public FtpRequest Request
        {
            get
            {
                return _request;
            }
        }
    }
}
