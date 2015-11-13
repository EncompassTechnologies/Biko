using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;

namespace Communications.Cryptography.OpenPGP
{
    public class GnuPGKeyCollection : IEnumerable<GnuPGKey>
    {
        private List<GnuPGKey> _keyList = new List<GnuPGKey>();
        private string _raw;
        private static string COL_KEY = "Key";
        private static string COL_KEY_EXPIRATION = "KeyExpiration";
        private static string COL_USER_ID = "UserId";
        private static string COL_USER_NAME = "UserName";
        private static string COL_SUB_KEY = "SubKey";
        private static string COL_SUB_KEY_EXPIRATION = "SubKeyExpiration";
        private static string COL_RAW = "Raw";

        public GnuPGKeyCollection(StreamReader keys)
        {
            Fill(keys);
            GetRaw(keys);
        }

        public string Raw
        {
            get
            {
                return _raw;
            }
        }

        private void GetRaw(StreamReader keys)
        {
            keys.BaseStream.Position = 0;
            _raw = keys.ReadToEnd();
        }

        private void Fill(StreamReader data)
        {
            string text = "";

            while (!data.EndOfStream)
            {
                string line = data.ReadLine();

                if (!line.StartsWith("pub") && !line.StartsWith("sec") && !line.StartsWith("uid"))
                {
                    if (text.Length != 0)
                    {
                        _keyList.Add(new GnuPGKey(text));
                        text = "";
                    }

                    continue;
                }

                text += line;
            }
        }

        public int IndexOf(GnuPGKey item)
        {
            return _keyList.IndexOf(item);
        }

        public GnuPGKey GetKey(int index)
        {
            return _keyList[index];
        }

        public void AddKey(GnuPGKey item)
        {
            _keyList.Add(item);
        }

        public int Count
        {
            get
            {
                return _keyList.Count;
            }
        }

        IEnumerator<GnuPGKey> IEnumerable<GnuPGKey>.GetEnumerator()
        {
            return _keyList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _keyList.GetEnumerator();
        }

        public GnuPGKey this[int index]
        {
            get
            {
                return _keyList[index];
            }

        }

        public DataTable ToDataTable()
        {
            DataTable dataTbl = new DataTable();
            CreateColumns(dataTbl);

            foreach (GnuPGKey item in _keyList)
            {
                DataRow row = dataTbl.NewRow();
                row[COL_USER_ID] = item.UserId;
                row[COL_USER_NAME] = item.UserName;
                row[COL_KEY] = item.Key;
                row[COL_KEY_EXPIRATION] = item.KeyExpiration;
                row[COL_SUB_KEY] = item.SubKey;
                row[COL_SUB_KEY_EXPIRATION] = item.SubKeyExpiration;
                dataTbl.Rows.Add(row);
            }

            return dataTbl;
        }

        private void CreateColumns(DataTable dataTbl)
        {
            dataTbl.Columns.Add(new DataColumn(COL_USER_ID, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_USER_NAME, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_KEY, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_KEY_EXPIRATION, typeof(DateTime)));
            dataTbl.Columns.Add(new DataColumn(COL_SUB_KEY, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_SUB_KEY_EXPIRATION, typeof(DateTime)));
            dataTbl.Columns.Add(new DataColumn(COL_RAW, typeof(string)));
        }
    }
}
