using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class GetDirListDeepAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        private FtpItemCollection _directoryListing;

        public GetDirListDeepAsyncCompletedEventArgs(Exception error, bool canceled, FtpItemCollection directoryListing)
            : base(error, canceled, null)
        {
            _directoryListing = directoryListing;
        }

        public FtpItemCollection DirectoryListingResult
        {
            get
            {
                return _directoryListing;
            }
        }
    }
}
