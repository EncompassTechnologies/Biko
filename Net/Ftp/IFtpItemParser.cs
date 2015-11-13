using System;

namespace Communications.Net.Ftp
{
    public interface IFtpItemParser
    {
        FtpItem ParseLine(string line);
    }
}
