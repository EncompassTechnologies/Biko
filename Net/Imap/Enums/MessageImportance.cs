using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(Normal)]
    public enum MessageImportance
    {
        Normal,
        High,
        Medium,
        Low
    }
}