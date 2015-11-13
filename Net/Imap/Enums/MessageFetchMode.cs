using System;
using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(None), Flags]
    public enum MessageFetchMode
    {
        None = -2,
        ClientDefault = -1,
        Flags = 1,
        InternalDate = 2,
        Size = 4,
        Headers = 8,
        BodyStructure = 16,
        Body = BodyStructure | 32,
        Attachments = BodyStructure | 64,
        GMailMessageId = 128,
        GMailThreads = 256,
        GMailLabels = 512,
        GMailExtendedData = GMailMessageId | GMailLabels | GMailThreads,
        Tiny = Flags | Headers | BodyStructure,
        Minimal = Tiny | Size | InternalDate,
        Basic = Minimal | Body,
        Full = Basic | Attachments
    }
}