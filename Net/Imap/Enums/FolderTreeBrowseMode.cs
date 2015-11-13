using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(FolderTreeBrowseMode.Lazy)]
    public enum FolderTreeBrowseMode
    {
        Lazy,
        Full
    }
}