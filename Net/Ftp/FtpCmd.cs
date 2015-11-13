using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Communications.Net.Ftp
{
    public enum FtpCmd
    {
        Unknown,
        User,
        Pass,
        Mkd,
        Rmd,
        Retr,
        Pwd,
        Syst,
        Cdup,
        Dele,
        Type,
        Cwd,
        Port,
        Pasv,
        Stor,
        Stou,
        Appe,
        Rnfr,
        Rnto,
        Abor,
        List,
        Nlst,
        Site,
        Stat,
        Noop,
        Help,
        Allo,
        Quit,
        Rest,
        Auth,
        Pbsz,
        Prot,
        Mode,
        Mdtm,
        Size,
        Feat,
        Xcrc,
        Xmd5,
        Xsha1,
        Epsv,
        Erpt
    }
}
