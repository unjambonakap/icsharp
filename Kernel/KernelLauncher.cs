using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.Logging;
using Common.Logging.Configuration;
using iCSharp.Messages;
using log4net.Appender;
using log4net.Layout;
using Newtonsoft.Json;

namespace iCSharp.Kernel
{

    public class KernelLauncherConfig
    {
        public string KernelConfigJsonWriteDirectory { get; set; }

        public string Name { get; set; }

    }
    public class KernelLauncher : IDisposable
    {
        public static bool Running = false;

        IServer _shellServer;
        IServer _hbServer;
        ConnectionInformation _connectionInfo;
        private KernelLauncherConfig _config;

        public KernelLauncher(KernelLauncherConfig config)
        {
            _config = config;
        }
        public string Create(ExtraParams extraParams = null)
        {
            Running = true;

            var connectionInfo = new ConnectionInformation
            {
                //IP = "0.0.0.0",
                IP = "127.0.0.1",
                SignatureScheme = "hmac-sha256",
                Key = Guid.NewGuid().ToString(),
                Transport = "tcp",
                Name = _config.Name,
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


            string configFile = WriteConfig(connectionInfo);

            _connectionInfo = connectionInfo;

            _shellServer = shellServer;
            _hbServer = heartBeatServer;
            return configFile;

        }
        string WriteConfig(ConnectionInformation connectionInfo)
        {
            var oldName = connectionInfo.Name;
            string kernelConfigFile = null;
            int tryId = 0;
            try
            {

                var newName = $"{oldName}-{tryId}";
                connectionInfo.Name = newName;
                kernelConfigFile = Path.Combine(_config.KernelConfigJsonWriteDirectory, $"kernel{connectionInfo.Name}.json");
                using (var fs = new FileStream(kernelConfigFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    Debug.WriteLine($"Kernel config at {kernelConfigFile}");

                    byte[] data = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(connectionInfo));
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
                return kernelConfigFile;
            }
            catch (Exception)
            {
                connectionInfo.Name = oldName;
                throw;
            }
        }

        public void Dispose()
        {
            _shellServer.Dispose();
            _hbServer.Dispose();
        }


        public void Wait()
        {
            _shellServer.GetWaitEvent().Wait();
            _hbServer.GetWaitEvent().Wait();
        }

        public static void Init()
        {

            if (!log4net.LogManager.GetRepository().Configured)
            {
                foreach (var x in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                    Console.WriteLine($"Resource >> {x}");
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("iCSharp.Kernel.App.config"))
                {
                    if (stream == null)
                        throw new Exception("Bad config");
                    log4net.Config.XmlConfigurator.Configure(stream);
                }
                var y = ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository());
                var layout = new PatternLayout("%date{dd MMM yyyy HH:mm:ss,fff} [%thread] -() %message%newline");

                y.Root.AddAppender(new TextWriterAppender { Layout = layout, Writer = Console.Out });
            }

            var cx = new NameValueCollection();
            cx["configType"] = "INLINE";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(cx);
        }

        public static ExtraParams GetExtraParams()
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
            ExtraParams extraParams = new ExtraParams();
            extraParams.References.AddRange(extraRefs);
            extraParams.DllPaths.Add(curPath);
            extraParams.DllPaths.AddRange(extraRefs.Select(x => Path.GetDirectoryName(x)));
            extraParams.WhitelistedNamespaces = whitelistedNamespaces;
            extraParams.ExtraAssemblies.Add(Assembly.GetEntryAssembly());
            return extraParams;
        }

    }
}