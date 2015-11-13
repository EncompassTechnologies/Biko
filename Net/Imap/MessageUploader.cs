using Communications.Net.Imap.Parsing;
using System.Text;

namespace Communications.Net.Imap
{
    internal class MessageUploader : CommandProcessor
    {
        private string _eml;

        public MessageUploader(string eml)
        {
            _eml = eml;
            TwoWayProcessing = true;
        }

        public MessageUploader(Message msg)
            : this(msg.ToEml())
        {
        }

        public override void ProcessCommandResult(string data)
        {
        }

        public override byte[] AppendCommandData(string serverResponse)
        {
            return Encoding.UTF8.GetBytes((_eml ?? "") + "\r\n");
        }
    }
}