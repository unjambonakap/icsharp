using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;
using iCSharp.Kernel;
using Newtonsoft.Json;

namespace iCSharp.Test
{
    public class CodeMode
    {
        public static int Val = 123;



    }
    class Program
    {
        static void Main(string[] args)
        {
            var code = @"
using Newtonsoft.Json;
using System.Linq;
using iCSharp.Test;
using System;
using System.Collections.Generic;

public class Test2 {
    public int f1(){
        return 5;
    }

    
}
            Console.WriteLine(""From 1"");
            var abc = 123;
            CodeMode.Val = 444;
    Console.WriteLine( JsonConvert.SerializeObject(123));
    Console.WriteLine( new Test2().f1());
    var x = new List<int>{123,456};
    xyz;
    Console.WriteLine(_env.Context == null);
    Console.WriteLine( JsonConvert.SerializeObject(_env.Context.Settings.ReferencesLookupPaths));
    _env.Debug = true;
    ";
            var u = new List<int> { 1, 2, 3 };
            var code2 = @"
using System;
//using Newtonsoft.Json;
            Console.WriteLine($"">>> {abc}"");
    //        Console.WriteLine( JsonSerializer.Create().Serialize(123));
    //        return JsonSerializer.Create().Serialize(123);
            ";
            KernelLauncher.Init();
            var extraParams = KernelLauncher.GetExtraParams();
            extraParams = new ExtraParams();
            log4net.LogManager.GetLogger(typeof(KernelLauncher)).Warn("FUUU");

            extraParams.References.Add("/usr/lib/mono/4.5/mscorlib.dll");
            extraParams.References.Add("Newtonsoft.Json.dll");
            extraParams.References.Add("iCSharp.Test.exe");
            var logger = LogManager.GetLogger<Program>();
            logger.Warn("FUUU log4net test");

            if (true)
            {
                var config = new KernelLauncherConfig
                {
                    KernelConfigJsonWriteDirectory = "/tmp/kernels/",
                    Name = "test1",

                };

                var launcher = new KernelLauncher(config);
                var confFile = launcher.Create(extraParams);
                Console.WriteLine($"Wrote conf to {confFile}");
                launcher.Wait();
                return;
            }
            //extraParams.DllAllowedGlob.Add("System.Core*.dll");
            //extraParams.DllAllowedGlob.Add("benoit*.dll");
            var ser1 = JsonSerializer.Create();
            Console.WriteLine(JsonConvert.SerializeObject(extraParams));
            var replFac = new ReplEngineFactory(logger, new string[] { }, extraParams);
            var res1 = replFac.ReplEngine.Execute(code);
            Console.WriteLine($"Result >> {res1.ReturnValue}");
            Console.WriteLine($"Result >> {res1.IsError}");
            Console.WriteLine($"Result >> {res1.CompileError}");
            Console.WriteLine($"Result >> {res1.ExecuteError}");
            Console.WriteLine("Hello World!");
            Console.WriteLine(CodeMode.Val);
            var res2 = replFac.ReplEngine.Execute(@"
            _env.AddSearchPath(""/home/benoit/programmation/projects/csharp_sln1/Test/bin/Debug"");
            _env.AddReference(""Test1.Test1.dll"");
            var e = new Test1.Test.MyClass1();
            Console.WriteLine($"" xxx {e.GetMessage()}"");
            ");


            if (true)
            {
                var ax = Assembly.LoadFile("/home/benoit/repos/icsharp/Engine/newlib/System.Linq.4.1.0/ref/net463/System.Linq.dll");

                var lst = new[] {
                Assembly.LoadFile("/usr/lib/mono/4.5/mscorlib.dll"),
};
                var imports = new[] {
    "System",

};
                //var res2= CSharpScript.RunAsync(code2).Result;
                //Console.WriteLine($"Results >> {res1} xxx {res2}");
            }


        }
    }
}
