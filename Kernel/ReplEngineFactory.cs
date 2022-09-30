using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using iCSharp.Kernel.ScriptEngine;
using System.Runtime.ExceptionServices;

using IReplEngine = iCSharp.Kernel.ScriptEngine.IReplEngine;
using ILog = Common.Logging.ILog;
using System.Text.RegularExpressions;
using System.IO;

namespace iCSharp.Kernel
{
    public class ExtraParams
    {
        public object Data {get; set;}
        public List<string> References = new List<string>();
        public List<string> SearchPaths = new List<string>();
        public List<string> DllPaths = new List<string>();
        public List<string> DllAllowedGlob = new List<string>();
        public List<Assembly> ExtraAssemblies = new List<Assembly>();

        public List<string> WhitelistedNamespaces = new List<string>();
        public Action<Action> PushAction = x=> x();
    }

    public class ScriptResult
    {
        public static Regex BadNamespaceRegex =
            new Regex(@"error CS0246: The type or namespace name '(?<namespace>[^']+)'");

        private readonly HashSet<string> _invalidNamespaces = new HashSet<string>();

        public static readonly ScriptResult Empty = new ScriptResult();

        public static readonly ScriptResult Incomplete = new ScriptResult { IsCompleteSubmission = false };

        public ScriptResult()
        {
            // Explicit default ctor to use as mock return value.
            IsCompleteSubmission = true;
        }

        public ScriptResult(
            object returnValue = null,
            Exception executionException = null,
            Exception compilationException = null,
            IEnumerable<string> invalidNamespaces = null)
        {
            if (returnValue != null)
            {
                ReturnValue = returnValue;
            }

            if (executionException != null)
            {
                ExecuteExceptionInfo = executionException;
            }

            if (compilationException != null)
            {
                if (invalidNamespaces == null)
                {
                    var matches = BadNamespaceRegex.Matches(compilationException.ToString());
                    List<string> bad = new List<string>();
                    for (int i = 0; i < matches.Count; ++i)
                    {
                        var m = matches[i];
                        bad.Add(m.Groups["namespace"].Value);
                    }
                    invalidNamespaces = bad;

                }
                CompileExceptionInfo = compilationException;

            }

            if (invalidNamespaces != null)
            {
                foreach (var ns in invalidNamespaces.Distinct())
                {
                    _invalidNamespaces.Add(ns);
                }
            }

            IsCompleteSubmission = true;
        }

        public object ReturnValue { get; private set; }

        public Exception ExecuteExceptionInfo { get; private set; }

        public Exception CompileExceptionInfo { get; private set; }

        public IEnumerable<string> InvalidNamespaces
        {
            get
            {
                return _invalidNamespaces.ToArray();
            }
        }

        public bool IsCompleteSubmission { get; private set; }
    }

    public class AssemblyReferences
    {
        public readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();
        public readonly Dictionary<string, string> _paths = new Dictionary<string, string>();
        public readonly HashSet<string> _search_paths = new HashSet<string>();

        public AssemblyReferences()
            : this(Enumerable.Empty<string>())
        {
        }

        public AssemblyReferences(IEnumerable<Assembly> assemblies)
            : this(assemblies, Enumerable.Empty<string>())
        {
        }

        public AssemblyReferences(IEnumerable<string> paths)
            : this(Enumerable.Empty<Assembly>(), paths)
        {
        }

        public AssemblyReferences(IEnumerable<Assembly> assemblies, IEnumerable<string> paths)
            : this(assemblies, paths, Enumerable.Empty<string>())
        {
        }

        public AssemblyReferences(IEnumerable<Assembly> assemblies, IEnumerable<string> paths, IEnumerable<string> search_paths)
        {
            foreach (var x in search_paths) _search_paths.Add(x);

            foreach (var assembly in assemblies.Where(assembly => assembly != null))
            {
                var name = assembly.GetName().Name;
                if (!_assemblies.ContainsKey(name))
                {
                    _assemblies.Add(name, assembly);
                }
            }

            foreach (var path in paths)
            {
                if (path.EndsWith("/") || path.EndsWith("\\"))
                {
                    _search_paths.Add(path.Substring(0, path.Length - 1));
                    continue;
                }
                var name = Path.GetFileName(path);
                if (name == null)
                {
                    continue;
                }

                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    name = Path.GetFileNameWithoutExtension(name);
                }

                if (!_paths.ContainsKey(name) && !_assemblies.ContainsKey(name))
                {
                    _paths.Add(name, path);
                }
            }
        }

        public IEnumerable<Assembly> Assemblies
        {
            get { return _assemblies.Values.ToArray(); }
        }

        public IEnumerable<string> Paths
        {
            get { return _paths.Values.ToArray(); }
        }
        public IEnumerable<string> SearchPaths
        {
            get { return _search_paths.ToArray(); }
        }

        public AssemblyReferences Union(AssemblyReferences references)
        {

            return new AssemblyReferences(Assemblies.Union(references.Assemblies), Paths.Union(references.Paths), SearchPaths.Union(references.SearchPaths));
        }

        public AssemblyReferences Union(IEnumerable<Assembly> assemblies)
        {

            return new AssemblyReferences(Assemblies.Union(assemblies), Paths, SearchPaths);
        }

        public AssemblyReferences Union(IEnumerable<string> paths)
        {

            return new AssemblyReferences(Assemblies, Paths.Union(paths), SearchPaths);
        }
        public AssemblyReferences UnionSearchPaths(IEnumerable<string> search_paths)
        {

            return new AssemblyReferences(Assemblies, Paths, SearchPaths.Union(search_paths));
        }


        public AssemblyReferences Except(AssemblyReferences references)
        {

            return new AssemblyReferences(Assemblies.Except(references.Assemblies), Paths.Except(references.Paths), SearchPaths.Except(references.SearchPaths));
        }

        public AssemblyReferences Except(IEnumerable<Assembly> assemblies)
        {
            return new AssemblyReferences(Assemblies.Except(assemblies), Paths, SearchPaths);
        }

        public AssemblyReferences Except(IEnumerable<string> paths)
        {

            return new AssemblyReferences(Assemblies, Paths.Except(paths), SearchPaths);
        }
        public AssemblyReferences ExceptSearchPath(IEnumerable<string> search_paths)
        {
            return new AssemblyReferences(Assemblies, Paths, SearchPaths.Except(search_paths));
        }
    }
    public class ReplEngineFactory
    {
        private string[] args;

        private IReplEngine _replEngine;
        private MemoryBufferConsole _console;
        private ExtraParams _extraParams;
        private ILog _logger;

        public ReplEngineFactory(ILog logger, string[] args, ExtraParams extraParams)
        {
            this._logger = logger;
            this.args = args;
            _extraParams = extraParams;
            _console = new MemoryBufferConsole();
        }

        public IReplEngine ReplEngine
        {
            get
            {
                if (this._replEngine == null)
                {
                    this._replEngine = new ReplEngineWrapper(this.Logger, new MonoScriptEngine(), this.Console, this._extraParams);
                }

                return this._replEngine;
            }
        }

        public MemoryBufferConsole Console
        {
            get { return this._console; }
        }

        private ILog Logger
        {
            get { return this._logger; }
        }

        private static void SetProfile()
        {
            var profileOptimizationType = Type.GetType("System.Runtime.ProfileOptimization");
            if (profileOptimizationType != null)
            {
                var setProfileRoot = profileOptimizationType.GetMethod("SetProfileRoot", BindingFlags.Public | BindingFlags.Static);
                setProfileRoot.Invoke(null, new object[] { typeof(ReplEngineFactory).Assembly.Location });

                var startProfile = profileOptimizationType.GetMethod("StartProfile", BindingFlags.Public | BindingFlags.Static);
                startProfile.Invoke(null, new object[] { typeof(ReplEngineFactory).Assembly.GetName().Name + ".profile" });
            }
        }
    }
}
