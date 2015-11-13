using System;
using System.ComponentModel;

namespace Communications.Net.Ftp
{
    public class GetDirListAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        private FtpItemCollection _directoryListing;

        public GetDirListAsyncCompletedEventArgs(Exception error, bool canceled, FtpItemCollection directoryListing)
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
