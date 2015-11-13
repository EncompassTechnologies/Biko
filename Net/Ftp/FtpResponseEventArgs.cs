using System;

namespace Communications.Net.Ftp
{
    public class FtpResponseEventArgs : EventArgs
    {
        private FtpResponse _response;

        public FtpResponseEventArgs(FtpResponse response)
        {
            _response = response;
        }

        public FtpResponse Response
        {
            get
            {
                return _response;
            }
        }
    }
}
