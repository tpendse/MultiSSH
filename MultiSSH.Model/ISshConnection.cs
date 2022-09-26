using System;

namespace MultiSSH.Model
{
    public interface ISshConnection : IDisposable
    {
        bool IsDisposed { get; }

        string Command { get; set; }

        void Run();
        void Run(string command);
    }
}
