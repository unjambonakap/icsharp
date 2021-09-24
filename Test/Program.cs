using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Logging.Simple;
using iCSharp.Kernel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using ScriptCs.Contracts;

namespace iCSharp.Test
{
    public class CodeMode {
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

public class Test2 {
    public int f1(){
        return 5;
    }

    
}
using iCSharp.Test;
            Console.WriteLine(""From 1"");
            var abc = 123;
            CodeMode.Val = 444;
    Console.WriteLine( JsonConvert.SerializeObject(123));
    Console.WriteLine( new Test2().f1());
    new List<int>{123,456}";
            var u =new List<int>{1,2,3};
            var code2 = @"
using System;
//using Newtonsoft.Json;
            Console.WriteLine($"">>> {abc}"");
    //        Console.WriteLine( JsonSerializer.Create().Serialize(123));
    //        return JsonSerializer.Create().Serialize(123);
            ";
            var logger = new NoOpLogger();
            var extraParams = KernelLauncher.GetExtraParams();
            extraParams = new ExtraParams(); ;
            extraParams.References.Add("Newtonsoft.Json.dll");
            extraParams.References.Add("iCSharp.Test.exe");
            //extraParams.DllAllowedGlob.Add("System.Core*.dll");
            //extraParams.DllAllowedGlob.Add("benoit*.dll");
            var ser1 = JsonSerializer.Create();
            Console.WriteLine(JsonConvert.SerializeObject(extraParams));
            var replFac = new ReplEngineFactory(logger, new string[] { }, extraParams);
            var options = ScriptOptions.Default;
            var res1 = replFac.ReplEngine.Execute(code);
            Console.WriteLine($"Result >> {res1.ReturnValue}");
            Console.WriteLine($"Result >> {res1.IsError}");
            Console.WriteLine($"Result >> {res1.CompileError}");
            Console.WriteLine($"Result >> {res1.ExecuteError}");
            Console.WriteLine("Hello World!");
            Console.WriteLine(CodeMode.Val);

            if (false){
            var ax = Assembly.LoadFile("/home/benoit/repos/icsharp/Engine/newlib/System.Linq.4.1.0/ref/net463/System.Linq.dll");

var lst = new [] {
                Assembly.LoadFile("/usr/lib/mono/4.5/mscorlib.dll"),
};
var imports = new [] {
    "System",

};
            options = options.WithReferences(lst);
            options = options.AddReferences("System.dll");
            options = options.AddReferences("System.Linq.dll");
            options = options.AddImports(imports);
            var res = CSharpScript.RunAsync(code, options).Result;
            return;
            //var res2= CSharpScript.RunAsync(code2).Result;
            //Console.WriteLine($"Results >> {res1} xxx {res2}");
            }


        }
    }
}
