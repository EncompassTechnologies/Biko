using System;
using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(None), Flags]
    public enum BodyType
    {
        None = 1,
        Text = 2,
        Html = 4,
        TextAndHtml = Text | Html
    }
}