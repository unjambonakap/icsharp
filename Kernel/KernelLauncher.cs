using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using iCSharp.Messages;
using Newtonsoft.Json;
using ScriptCs;
using ScriptCs.Contracts;

namespace iCSharp.Kernel
{

    public class KernelLauncher : IDisposable
    {
        public static bool Running = false;

        object data;
        IServer _shellServer;
        IServer _hbServer;
        ConnectionInformation _connectionInfo;
        private string _kernelConfigFile;
        string name;
        FileStream _fs;

        public KernelLauncher(string name, object data)
        {
            this.data = data;
            this.name = name;
        }
        public void Create(ExtraParams extraParams = null)
        {
            ScriptEnvironment.GlobalData = data;
            Running = true;

            var connectionInfo = new ConnectionInformation
            {
                //IP = "0.0.0.0",
                IP = "127.0.0.1",
                SignatureScheme = "hmac-sha256",
                Key = Guid.NewGuid().ToString(),
                Transport = "tcp",
                Name = name,
            };

            extraParams = extraParams ?? GetExtraParams();
            KernelCreator creator = new KernelCreator(connectionInfo, extraParams);

            IServer shellServer = creator.ShellServer;
            shellServer.StartRandomPort();

            IServer heartBeatServer = creator.HeartBeatServer;
            heartBeatServer.StartRandomPort();




            connectionInfo.IOPubPort = shellServer.PortMapping["iopub"];
            connectionInfo.ShellPort = shellServer.PortMapping["shell"];
            connectionInfo.ControlPort = shellServer.PortMapping["control"];

            connectionInfo.HBPort = heartBeatServer.PortMapping["hb"];


            for (int i = 0; ; ++i)
            {
                if (TryWriteConfig(connectionInfo, i)) break;
            }

            _connectionInfo = connectionInfo;

            _shellServer = shellServer;
            _hbServer = heartBeatServer;
        }

        bool TryWriteConfig(ConnectionInformation connectionInfo, int tryId)
        {
            var path = System.Environment.GetEnvironmentVariable("JUPYTER_KERNEL_DIR");
            var oldName = connectionInfo.Name;
            try
            {

                var newName = $"{oldName}-{tryId}";
                connectionInfo.Name = newName;
                _kernelConfigFile = Path.Combine(path, $"kernel{connectionInfo.Name}.json");
                _fs = new FileStream(_kernelConfigFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                Debug.WriteLine($"Kernel config at {_kernelConfigFile}");

                byte[] data = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(connectionInfo));
                _fs.Write(data, 0, data.Length);
                _fs.Flush();
                return true;
            }
            catch (Exception)
            {
                connectionInfo.Name = oldName;
                Debug.WriteLine($"Kernel config at {_kernelConfigFile} already exists for tryid={tryId}");
                return false;
            }
        }

        public void Dispose()
        {
            _shellServer.Dispose();
            _hbServer.Dispose();
            _fs.Dispose();
        }


        public void Wait()
        {
            _shellServer.GetWaitEvent().Wait();
            _hbServer.GetWaitEvent().Wait();
        }


        public static ScriptCs.Contracts.ExtraParams GetExtraParams()
        {
            var extraRefsAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic)
                .Where(x => x.Location != "")
                .Where(x => File.Exists(x.Location))
                .ToList();

            var extraRefs = extraRefsAssemblies.Select(x => x.Location).ToList();
            var curPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var whitelistedNamespaces = new List<string> {
            "System.Linq", "System.Reflection", "System.Text", "Newtonsoft.Json", "Newtonsoft.Json.Linq",
            "System.Threading",  "MoreLinq",
            };

            ScriptCs.Contracts.ExtraParams extraParams = new ScriptCs.Contracts.ExtraParams();
            extraParams.References.AddRange(extraRefs);
            extraParams.DllPaths.Add(curPath);
            extraParams.DllPaths.AddRange(extraRefs.Select(x => Path.GetDirectoryName(x)));
            extraParams.WhitelistedNamespaces = whitelistedNamespaces;
            extraParams.ExtraAssemblies.Add(Assembly.GetEntryAssembly());
            return extraParams;
        }

    }
}