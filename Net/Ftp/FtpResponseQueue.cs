using System;
using System.Collections.Generic;
using System.Text;

namespace Communications.Net.Ftp
{
    internal class FtpResponseQueue
    {
        private Queue<FtpResponse> _queue = new Queue<FtpResponse>(10);

        public int Count
        {
            get
            {
                lock (this)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(FtpResponse item)
        {
            lock (this)
            {
                _queue.Enqueue(item);
            }
        }

        public FtpResponse Dequeue()
        {
            lock (this)
            {
                return _queue.Dequeue();
            }
        }
    }
}
