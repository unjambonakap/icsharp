

using Common.Logging;

namespace iCSharp.Kernel.Heartbeat
{
    using NetMQ;
    using NetMQ.Sockets;
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;

    public class Heartbeat : IServer
    {
        private ILog logger;
        private string address;

        private ResponseSocket server;

        private ManualResetEventSlim stopEvent;

        private Thread thread;

        private bool disposed;

        public Dictionary<string, int> PortMapping { get; } = new Dictionary<string, int>();

        public Heartbeat(ILog logger,  string address)
        {
            this.logger = logger;
            this.address = address;

            this.server = new ResponseSocket();
            this.stopEvent = new ManualResetEventSlim();
        }

        public void Start()
        {
            this.server.Bind(this.address);
            this.thread = new Thread(this.StartServerLoop);
            this.thread.Start();
            //ThreadPool.QueueUserWorkItem(new WaitCallback(StartServerLoop));
        }
        public void StartRandomPort()
        {
            PortMapping["hb"] = this.server.BindRandomPort(this.address);
            this.thread = new Thread(this.StartServerLoop);
            this.thread.Start();
        }

        public void Stop()
        {
            this.stopEvent.Set();
        }

        public ManualResetEventSlim GetWaitEvent()
        {
            return this.stopEvent;
        }

        private void StartServerLoop(object state)
        {

            try
            {

                while (!this.stopEvent.Wait(0))
                {
                    byte[] data = this.server.ReceiveFrameBytes();

                    this.logger.Info(System.Text.Encoding.Default.GetString(data));
                    // Echoing back whatever was received
                    this.server.TrySendFrame(data);
                }
            }
            catch (SocketException s) { }
            catch (ObjectDisposedException e) { }

        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            this.Stop();
            if (!this.disposed)
            {
                if (dispose)
                {
                    if (this.server != null)
                    {
                        this.server.Dispose();
                    }

                    this.disposed = true;
                }
            }
        }

    }
}
