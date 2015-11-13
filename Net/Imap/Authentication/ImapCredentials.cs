using Communications.Net.Imap.Parsing;

namespace Communications.Net.Imap.Authentication
{
    public abstract class ImapCredentials : CommandProcessor
    {
        public abstract string ToCommand(Capability capabilities);

        public abstract bool IsSupported(Capability capabilities);
    }
}