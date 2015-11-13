namespace Communications.Net.Imap.Constants
{
    public sealed class MessageHeaderSets
    {
        public static readonly string[] Minimal =
        {
            MessageHeader.From,
            MessageHeader.To,
            MessageHeader.Date,
            MessageHeader.Subject,
            MessageHeader.Cc,
            MessageHeader.ContentType
        };
    }
}