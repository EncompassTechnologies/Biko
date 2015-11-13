using Communications.Net.Imap.Enums;
using System.Text;

namespace Communications.Net.Imap.EncodingHelpers
{
    internal class BodyDecoder
    {
        public static string DecodeMessageContent(MessageContent content)
        {
            Encoding encoding = Encoding.UTF8;
            try
            {
                encoding = Encoding.GetEncoding(content.ContentType.CharSet);
            }
            catch
            {
            }

            switch (content.ContentTransferEncoding)
            {
                case ContentTransferEncoding.Base64:
                    return StringDecoder.DecodeBase64(content.ContentStream, encoding);

                case ContentTransferEncoding.QuotedPrintable:
                    return StringDecoder.DecodeQuotedPrintable(content.ContentStream, encoding);

                default:
                    return content.ContentStream;
            }
        }
    }
}