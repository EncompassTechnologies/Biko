using System;

namespace Communications.Net.Imap.Parsing
{
    public abstract class CommandProcessor
    {
        public bool TwoWayProcessing
        {
            get;
            protected set;
        }

        public abstract void ProcessCommandResult(string data);

        public virtual byte[] AppendCommandData(string serverResponse)
        {
            throw new NotImplementedException();
        }
    }
}