using System.ComponentModel;

namespace Communications.Net.Imap.Enums
{
    [DefaultValue(Off)]
    public enum IdleState
    {
        Off = 0,
        On = 1,
        Paused = 2,
        Starting = 4
    }
}