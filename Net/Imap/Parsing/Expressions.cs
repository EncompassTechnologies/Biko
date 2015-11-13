using System.Text.RegularExpressions;

namespace Communications.Net.Imap.Parsing
{
    public static class Expressions
    {
        public static readonly Regex FolderParseRex = new Regex(@".*\((\\.*)+\)\s[""]?(.|[NIL]{3})[""]?\s[""]?([^""]*)[""]?", RegexOptions.IgnoreCase);

        public static readonly Regex ExistsRex = new Regex(@"\* (\d{1,}) EXISTS");
        public static readonly Regex RecentRex = new Regex(@"\* (\d{1,}) RECENT");
        public static readonly Regex UnseenRex = new Regex(@"\* (\d{1,}) UNSEEN");
        public static readonly Regex UIdValidityRex = new Regex(@"UIDVALIDITY (\d{1,})");
        public static readonly Regex UIdNextRex = new Regex(@"UIDNEXT (\d{1,})");
        public static readonly Regex CopyUIdRex = new Regex(@"COPYUID (^\s*) (\d{1,}) (\d{1,})");
        public static readonly Regex StatusRex = new Regex(@"(\w+)\s(\d+)");

        public static readonly Regex PermanentFlagsRex = new Regex(@"\* OK \[PERMANENTFLAGS \((.*)\)\]");
        public static readonly Regex SearchRex = new Regex(@"\* SEARCH ([\d\s]+)");

        public static readonly Regex HeaderRex = new Regex(@"BODY\[HEADER(\.FIELDS.*?)?\] \{\d+\}$");
        public static readonly Regex HeaderParseRex = new Regex(@"^(\w.{1,}?):[\s]?(.{0,})");

        public static readonly Regex BodyStructRex = new Regex(@"BODYSTRUCTURE (\(.+\))");
        public static readonly Regex SizeRex = new Regex(@"RFC822.SIZE (\d+)");
        public static readonly Regex FlagsRex = new Regex(@"FLAGS \((.*?)\)");
        public static readonly Regex InternalDateRex = new Regex("INTERNALDATE \"(.+?)\"");

        public static readonly Regex IdleResponseRex = new Regex(@"\[*]? (\d{1,}) (\w+)");

        public static readonly Regex UTF7EncodeRex = new Regex("[^ -~]*");
        public static readonly Regex UTF7DecodeRex = new Regex(@"&[\w|,|\+]*-");
        public static readonly Regex StringEncodingRex = new Regex(@"[=]?\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=\s?");

        public static readonly Regex GMailLabelsRex = new Regex(@"X-GM-LABELS \((.*?)\)");
        public static readonly Regex GMailLabelSplitRex = new Regex(@"("".*?""|[^""\s]+)+(?=\s*|\s*$)");
        public static readonly Regex GMailThreadRex = new Regex(@"X-GM-THRID (\d+)");
        public static readonly Regex GMailMessageIdRex = new Regex(@"X-GM-MSGID (\d+)");

        public static readonly Regex ServerAlertRex = new Regex(@"\[ALERT\]\s(.*)$", RegexOptions.CultureInvariant);

        public static readonly Regex HtmlTagFilterRex = new Regex("<.*?>", RegexOptions.CultureInvariant | RegexOptions.Multiline);
        public static readonly Regex BrTagFilterRex = new Regex("<br.*?>", RegexOptions.CultureInvariant | RegexOptions.Multiline);
    }
}