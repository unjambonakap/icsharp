extern alias MonoCSharp;
using System.Text;
using System.Collections.Generic;

using ILog = Common.Logging.ILog;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using MonoCSharp::Mono.CSharp;
using ScriptCs.Engine.Mono.Segmenter;
using Common.Logging;
using Common.Logging.Simple;

namespace iCSharp.Kernel.ScriptEngine
{
    public interface IScriptEngine
    {
        string BaseDirectory { get; set; }

        string CacheDirectory { get; set; }

        string FileName { get; set; }

        ScriptResult Execute(
            string code,
            string[] scriptArgs,
             ExtraParams extra_params)
        ;
    }
    public class ScriptEnvironment
    {
        public static ScriptEnvironment _env { get; set; }
        public AssemblyReferences References { get; set; }
        public CompilerContext Context { get; set; }
        public bool Debug { get; set; } = false;
        public Evaluator Evaluator { get; internal set; }

        public void AddSearchPath(string path)
        {
            Context.Settings.ReferencesLookupPaths.Add(path);
        }
        public void AddReference(string reference)
        {

            Evaluator.LoadAssembly(reference);
        }

    }
    public class MonoScriptEngine : IScriptEngine
    {
        public const string SessionKey = "MonoSession";
        private ILog _log = new ConsoleOutLogger("kernel", LogLevel.Info, true, true, false, "yyyy/MM/dd HH:mm:ss:fff");

        public string BaseDirectory { get; set; }
        public string CacheDirectory { get; set; }
        public string FileName { get; set; }
        public Evaluator Evaluator { get; set; }

        public AssemblyReferences References { get; set; } = new AssemblyReferences();
        public ScriptEnvironment Env = new ScriptEnvironment();

        public MonoScriptEngine()
        {
            Env.References = References;
        }

        public ICollection<string> GetLocalVariables()
        {
            var vars = Evaluator.GetVars();
            if (!string.IsNullOrWhiteSpace(vars) && vars.Contains(Environment.NewLine))
            {
                return vars.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }

            return new Collection<string>();
        }

        public ScriptResult Execute(
            string code,
            string[] scriptArgs,
             ExtraParams extra_params)
        {

            if (Env.Debug)
                _log = new ConsoleOutLogger("kernel", LogLevel.Debug, true, true, false, "yyyy/MM/dd HH:mm:ss:fff");
            else
                _log = new ConsoleOutLogger("kernel", LogLevel.Info, true, true, false, "yyyy/MM/dd HH:mm:ss:fff");

            if (Evaluator == null)
            {
                //code = code.DefineTrace();
                _log.Debug("Creating session");
                var context = new CompilerContext(
                    new CompilerSettings { AssemblyReferences = References.Paths.Concat(extra_params.References).ToList() },
                    new ConsoleReportPrinter());
                // new ConsoleReportPrinter());

                Evaluator = new Evaluator(context);
                var allNamespaces = Namespaces;

                Evaluator.ReferenceAssembly(typeof(ScriptEnvironment).Assembly);
                Evaluator.InteractiveBaseClass = typeof(ScriptEnvironment);
                Env.Context = context;
                Env.Evaluator = Evaluator;
                ScriptEnvironment._env = Env;

                ImportNamespaces(allNamespaces, Evaluator);
            }
            else
            {
                _log.Debug("Reusing existing session");

                var newReferences = References;

                foreach (var reference in newReferences.Paths)
                {
                    _log.DebugFormat("Adding reference to {0}", reference);
                    Evaluator.LoadAssembly(reference);
                }


                var newNamespaces = Namespaces;

                ImportNamespaces(newNamespaces, Evaluator);
            }
            _log.Debug("Starting execution");
            var result = Execute(code, Evaluator);
            _log.Debug("Finished execution");

            return result;
        }


        protected virtual ScriptResult Execute(string code, Evaluator session)
        {

            try
            {
                object scriptResult = null;
                var segmenter = new ScriptSegmenter();
                foreach (var segment in segmenter.Segment(code))
                {
                    _log.Debug($"Executing code={segment.Code} ");
                    if (true)
                    {
                        bool resultSet;
                        string s = session.Evaluate(segment.Code, out scriptResult, out resultSet);
                    }
                    else
                    {
                        var res = session.Run(segment.Code);
                    }
                }


                return new ScriptResult(returnValue: scriptResult);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"exception {ex}");
                return new ScriptResult(executionException: ex.InnerException);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exception {ex}");
                return new ScriptResult(executionException: ex);
            }
        }

        private void ImportNamespaces(IEnumerable<string> namespaces, Evaluator sessionState)
        {
            var builder = new StringBuilder();
            foreach (var ns in namespaces)
            {
                _log.DebugFormat(ns);
                builder.AppendLine(string.Format("using {0};", ns));
                Namespaces.Add(ns);
            }
            Console.WriteLine(builder.ToString());

            sessionState.Compile(builder.ToString());
        }
        public HashSet<string> Namespaces = new HashSet<string>();
    }
    internal class ReplEngineWrapper : IReplEngine
    {
        private readonly ILog logger;
        private readonly IScriptEngine repl;
        private readonly MemoryBufferConsole console;
        public ExtraParams ExtraParams { get; set; }

        public ReplEngineWrapper(ILog logger, IScriptEngine repl, MemoryBufferConsole console, ExtraParams extraParams)
        {
            this.logger = logger;
            this.repl = repl;
            this.console = console;
            ExtraParams = extraParams;
        }

        public ExecutionResult Execute(string script)
        {
            this.console.ClearAllInBuffer();

            ScriptResult scriptResult = this.repl.Execute(script, new string[] { }, ExtraParams);

            ExecutionResult executionResult = new ExecutionResult()
            {
                OutputResultWithColorInformation = this.console.GetAllInBuffer(),
                CompileError = scriptResult.CompileExceptionInfo != null ? scriptResult.CompileExceptionInfo : null,
                ExecuteError = scriptResult.ExecuteExceptionInfo != null ? scriptResult.ExecuteExceptionInfo : null,
                ReturnValue = scriptResult.ReturnValue,
            };

            return executionResult;
        }

        private bool IsCompleteResult(ScriptResult scriptResult)
        {
            return scriptResult.ReturnValue != null && !string.IsNullOrEmpty(scriptResult.ReturnValue.ToString());
        }
    }
}
