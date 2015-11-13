using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Communications.Net.Ftp
{
    public class FtpItemCollection : IEnumerable<FtpItem>
    {
        private List<FtpItem> _list = new List<FtpItem>();
        private long _totalSize;

        private static string COL_NAME = "Name";
        private static string COL_MODIFIED = "Modified";
        private static string COL_SIZE = "Size";
        private static string COL_SYMBOLIC_LINK = "SymbolicLink";
        private static string COL_TYPE = "Type";
        private static string COL_ATTRIBUTES = "Attributes";
        private static string COL_RAW_TEXT = "RawText";

        public FtpItemCollection()
        {
        }

        public FtpItemCollection(string fileMask, string path, string fileList, IFtpItemParser itemParser)
        {
            Parse(fileMask, path, fileList, itemParser);
        }

        public void Merge(FtpItemCollection items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items", "must have a value");
            }

            foreach (FtpItem item in items)
            {
                FtpItem newItem = new FtpItem(item.Name, item.Modified, item.Size, item.SymbolicLink, item.Attributes, item.ItemType, item.RawText);
                newItem.ParentPath = item.ParentPath;
                this.Add(newItem);
            }
        }

        private void Parse(string fileMask, string path, string fileList, IFtpItemParser itemParser)
        {
            string[] lines = SplitFileList(fileList);
            Regex mask = new Regex(fileMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
            int length = lines.Length - 1;

            for (int i = 0; i <= length; i++)
            {
                FtpItem item = itemParser.ParseLine(lines[i]);

                if (item != null && mask.IsMatch(item.Name) && item.Name != "." & item.Name != "..")
                {
                    item.ParentPath = path;
                    _list.Add(item);
                    _totalSize += item.Size;
                }
            }
        }

        private string[] SplitFileList(string response)
        {
            char[] crlfSplit = new char[2];
            crlfSplit[0] = '\r';
            crlfSplit[1] = '\n';
            return response.Split(crlfSplit, StringSplitOptions.RemoveEmptyEntries);
        }

        public DataTable ToDataTable()
        {
            DataTable dataTbl = new DataTable();
            dataTbl.Locale = CultureInfo.InvariantCulture;
            CreateColumns(dataTbl);

            foreach (FtpItem item in _list)
            {
                DataRow row = dataTbl.NewRow();
                row[COL_NAME] = item.Name;
                row[COL_MODIFIED] = item.Modified;
                row[COL_SIZE] = item.Size;
                row[COL_SYMBOLIC_LINK] = item.SymbolicLink;
                row[COL_TYPE] = item.ItemType.ToString();
                row[COL_ATTRIBUTES] = item.Attributes;
                row[COL_RAW_TEXT] = item.RawText;
                dataTbl.Rows.Add(row);
            }

            return dataTbl;
        }

        private void CreateColumns(DataTable dataTbl)
        {
            dataTbl.Columns.Add(new DataColumn(COL_NAME, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_MODIFIED, typeof(DateTime)));
            dataTbl.Columns.Add(new DataColumn(COL_SIZE, typeof(long)));
            dataTbl.Columns.Add(new DataColumn(COL_TYPE, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_ATTRIBUTES, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_SYMBOLIC_LINK, typeof(string)));
            dataTbl.Columns.Add(new DataColumn(COL_RAW_TEXT, typeof(string)));
        }

        public long TotalSize
        {
            get
            {
                return _totalSize;
            }
        }

        public int IndexOf(FtpItem item)
        {
            return _list.IndexOf(item);
        }

        public void Add(FtpItem item)
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

        IEnumerator<FtpItem> IEnumerable<FtpItem>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public FtpItem this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public bool ContainsName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "must have a value");
            }

            foreach (FtpItem item in _list)
            {
                if (name == item.Name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
