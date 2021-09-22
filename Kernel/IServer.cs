
using System;
using System.Collections.Generic;
using System.Threading;

namespace iCSharp.Kernel
{
    public interface IServer : IDisposable
    {
        Dictionary<string, int> PortMapping { get; }
        void Start();
        void StartRandomPort();

        void Stop();

        ManualResetEventSlim GetWaitEvent();
    }
}
