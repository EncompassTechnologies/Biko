using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(None)]
    public enum MessageSensitivity
    {
        None,
        Personal,
        Private,
        CompanyConfidential
    }
}