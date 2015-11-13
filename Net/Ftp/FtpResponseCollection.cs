using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Communications.Net.Ftp
{
    public class FtpResponseCollection : IEnumerable<FtpResponse>
    {
        private List<FtpResponse> _list = new List<FtpResponse>();

        public FtpResponseCollection()
        {
        }

        public int IndexOf(FtpResponse item)
        {
            return _list.IndexOf(item);
        }

        public void Add(FtpResponse item)
        {
            _list.Add(item);
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        IEnumerator<FtpResponse> IEnumerable<FtpResponse>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public FtpResponse this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public string GetRawText()
        {
            StringBuilder builder = new StringBuilder();

            foreach (FtpResponse item in _list)
            {
                builder.Append(item.RawText);
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        public FtpResponse GetLast()
        {
            if (_list.Count == 0)
            {
                return new FtpResponse();
            }
            else
            {
                return _list[_list.Count - 1];
            }
        }
    }
}
