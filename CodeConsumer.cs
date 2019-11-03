using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using Microsoft.CSharp.RuntimeBinder;
using System.Threading;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleSharp
{
    public static class CodeConsumer // This is actually so that this class can be accessed by the compiled assembly at runtime.
    {
        public static byte OUTPUT_TYPE = 0;
        public static bool function_declaration = false;
        public static int cur_scope = 0;
        public static bool IS_EXE_OUTPUT()
        {
            return OUTPUT_TYPE == 2;
        }

        public static readonly string tryinputkey = @"
        public static ConsoleKeyInfo __TryInputK() {
            if(Console.KeyAvailable) {
                return Console.ReadKey(true);
            } else {
                return new ConsoleKeyInfo('\0', ConsoleKey.LaunchMediaSelect, false, false, false);
            }
        }
        ";
        public static readonly string iskeydown = @"
        public static bool __IsKD(ConsoleKey k) {
            Keys ks = PInvoke.KeyConvert(k);
            return PInvoke.IsKeyDown(ks);
        }
        ";
        public static readonly string characterat = @"
        public static char __FetchCharAt(int x, int y) {
            return PInvoke.ReadCharacter(x, y);
        }
        ";
        public static readonly string fetchcenter = @"
        public static int __FetchCenter(int dim) {
            if(dim == 0) {
                return Console.WindowWidth / 2;
            }
            if(dim == 1) {
                return Console.WindowHeight / 2;
            }
            return 0;
        }
        ";
        public static readonly string roundint = @"
        public static int __roundInt(double d) {
            return (int)Math.Round(d);
        }
        public static int __roundInt(decimal d) {
            return (int)Math.Round(d);
        }
        ";

        // Parsing Methods
        public static int FindClosingParenthesis(string code, int openerIndex)
        {
            int first = 1;
            int skips = 1;
            string ln = code;
            for (int b = 0; b < ln.Length; b++)
            {
                if (first > 0)
                {
                    // first loop
                    b = openerIndex + 1;
                    first--;
                }
                try
                {
                    char c = ln[b];
                    if (c.Equals('('))
                    {
                        skips++;
                        continue;
                    }
                    if (c.Equals(')'))
                    {
                        skips--;
                        if (skips <= 0)
                        {
                            return b;
                        }
                        continue;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }
            }
            Console.WriteLine("Something went wrong with parenthesis finding... skips=" + skips);
            Console.ReadLine();
            return -1;
        }
        private static bool InParenthesis(string ln, int pos)
        {
            int first = ln.IndexOf('(');
            if(first >= pos || first == -1)
            {
                return false;
            }
            int second = FindClosingParenthesis(ln, first);
            if(second == -1 || second <= pos)
            {
                return false;
            }
            return true;
        }
        public static Dictionary<string, string> conversions;
        private static SyntaxTree systree;

        public static StringBuilder function_buffer;
        public static List<string> function_list;
        public static List<string> function_names;
        public static List<string> struct_names;
        public static List<Assembly> refs = new List<Assembly>();

        public static StringBuilder sb;
        private static void _AddLine(string ln)
        {
            if (sb == null) { return; }
            if (sb.Length > 0)
            {
                sb.Append("\n" + ln);
            } else {
                sb.Append(ln);
            }
        }
        static readonly Dictionary<string, int> scope = new Dictionary<string, int>();
        public static void _CompilerMessage(string ln)
        {
            ln = ln.Trim().ToLower();
            if(ln.StartsWith('#'))
                ln = ln.Substring(1); // remove the #

            if(ln.StartsWith("output "))
            {
                string type = ln.Split(' ')[1];
                switch(type)
                {
                    case "dll":
                        OUTPUT_TYPE = 1;
                        break;
                    case "exe":
                        OUTPUT_TYPE = 2;
                        break;
                    case "code":
                        OUTPUT_TYPE = 3;
                        break;
                    default:
                        Console.Error.WriteLine("Invalid output type received.");
                        OUTPUT_TYPE = 0;
                        break;
                }
            } else if(ln.StartsWith("reference "))
            {
                string refname = ln.Split(' ')[1];
                if(!File.Exists(refname))
                {
                    Console.WriteLine("ERROR: Reference \"" + refname + "\" file does not exist. Check the file name and extension!");
                    Console.ReadLine();
                    Environment.Exit(0);
                    return;
                }
                try
                {
                    Assembly asm = Assembly.LoadFrom(refname);
                    Console.ForegroundColor = ConsoleColor.Green;
                    foreach(Type t in asm.GetTypes())
                    {
                        Console.WriteLine("Loaded Assembly Type: " + t.Name);
                        foreach(MethodInfo mi in t.GetMethods())
                        {
                            Console.WriteLine("    WithMethod: " + mi.Name);
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    refs.Add(asm);
                } catch(BadImageFormatException)
                {
                    Console.WriteLine("ERROR: The reference \"" + refname + "\" is not a valid assembly (not compatible with simplesharp!).");
                    Console.ReadLine();
                    Environment.Exit(0);
                    return;
                }
                
            }
            return;
        }
        private static string _ConvertAssignment(string ln)
        {
            string[] arr = ln.Split('=');
            string left = arr[0].Trim();
            string right = arr[1].Trim();
            if (left.EndsWith("]"))
            {
                return ln;
            }

            if (!scope.ContainsKey(left))
            {
                // Must be a declaration
                scope.Add(left, cur_scope);
            }
            else
            {
                // Already defined in scope.
                scope.TryGetValue(left, out int sc);
                if(cur_scope >= sc)
                {
                    return left + " = " + right;
                }
            }
            if (right.ToLower().Equals("null"))
            {
                return "dynamic " + left + ";";
            }
            if (!left.StartsWith("dynamic"))
            {
                left = "dynamic " + left;
            }
            return left + "=" + right;
        }
        private static string _PreStatementAliases(string ln)
        {
            if (ln.ToLower().StartsWith("try("))
            {
                string _i = GetParenthesisContents(ln, "try", out _);
                AddLine("try {");
                AddLine(_i);
                AddLine("} catch(Exception) {");
                return null;
            }
            return ln;
        }
        private static string _StatementAliases(string ln)
        {
            ln = ln.Trim();
            if (ln.ToLower().StartsWith("forevery(", StringComparison.OrdinalIgnoreCase))
            {
                string _i = GetParenthesisContents(ln, "forevery", out _);
                string[] spl = _i.Split(" in ");
                string left = spl[0];
                string right = spl[1];
                ln = "foreach(var " + left + " in " + right + ") {";
                return ln;
            }
            if (ln.ToLower().StartsWith("repeat(", StringComparison.OrdinalIgnoreCase))
            {
                string _i = GetParenthesisContents(ln, "repeat", out _);
                ln = "for(int i = 0; i < " + _i + "; i++) {";
                return ln;
            }
            if (ln.ToLower().Contains("array(", StringComparison.OrdinalIgnoreCase))
            {
                string _i = GetParenthesisContents(ln, "array", out int i);
                ln = ln.Remove(i) + "new dynamic[] { " + _i + " };";
                return ln;
            }
            if (ln.ToLower().Contains("list(", StringComparison.OrdinalIgnoreCase))
            {
                string _i = GetParenthesisContents(ln, "list", out int i);
                ln = ln.Remove(i) + "new List<dynamic> { " + _i + " };";
                return ln;
            }
            foreach(string struc in struct_names)
            {
                if (ln.ToLower().Contains(struc + "(", StringComparison.OrdinalIgnoreCase))
                {
                    string _i = GetParenthesisContents(ln, struc, out int i);
                    ln = ln.Remove(i) + "new " + struc + "(" + _i + ");";
                    return ln;
                }
            }
            foreach (KeyValuePair<string, string> kv in conversions)
            {
                var x = new List<dynamic> { 5, "" };
                string key = kv.Key;
                string sample = ln.ToLower();
                if(key.Equals("parse("))
                {
                    if(sample.Contains("int.parse("))
                    {
                        continue;
                    }
                }
                if(sample.Contains(key))
                {
                    ln = ln.Replace(key, kv.Value, StringComparison.OrdinalIgnoreCase);
                }
            }
            foreach(Assembly asm in refs)
            {
                foreach(Type tp in asm.GetTypes())
                {
                    string comp = tp.Name;
                    ln = ln.Replace(comp, comp, StringComparison.OrdinalIgnoreCase);
                    foreach(MethodInfo inf in tp.GetMethods())
                    {
                        string comp2 = inf.Name;
                        ln = ln.Replace(comp2, comp2, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            string b4 = ln;
            foreach (string cls in ReferenceManager.referencedClasses)
            {
                ln = ln.Replace(cls + ".", cls + ".", StringComparison.OrdinalIgnoreCase);
            }
            foreach (string mth in ReferenceManager.referencedMethods)
            {
                if(mth.Equals("If")) { continue; }
                ln = ln.Replace(mth + "(", mth + "(", StringComparison.OrdinalIgnoreCase);
            }
            foreach (string fld in ReferenceManager.referencedFields)
            {
                ln = ln.Replace("." + fld, "." + fld, StringComparison.OrdinalIgnoreCase);
            }
            if(!b4.Equals(ln))
            {
                Console.WriteLine("CORRECTED: {0}", ln);
            }
            return ln;
        }
        private static void _FunctionDeclaration(string ln)
        {
            // This whole thing is a literal crapshow of code!
            // probably shouldn't look at this too long because it's
            // hard for even me to look at lol

            function_declaration = true;
            string _s = ln.Trim();
            // second part without the word 'function'. ex: helloworld(blah) {
            string s = _s.Substring(_s.IndexOf(' ') + 1);

            int i = s.IndexOf("(", StringComparison.OrdinalIgnoreCase);
            string sub = s.Substring(i + 1);
            int c = sub.LastIndexOf(')');
            if (c == -1) { Console.Error.WriteLine("The line \"" + s + "\" is missing a closing parenthesis!"); return; }
            string _i = sub.Substring(0, c); // _i is the arguments of the function.

            // put dynamic before each argument
            string[] args = _i.Split(",");
            for(int x = 0; x < args.Length; x++)
            {
                string arg = args[x];
                args[x] = "dynamic " + arg.Trim();
            }

            // join them back together and structure it
            _i = string.Join(", ", args);
            s = "        public static dynamic " + s.Remove(s.IndexOf('(')) + "(" + _i + ") {\n";

            // get function name and store it globally
            // (for case INsensitivity support)
            string name = ln.Split(' ')[1].Remove(i);
            function_names.Add(name);

            // append the line to the fBuffer
            function_buffer.Append(s);
        }
        private static void _StructureDeclaration(string ln)
        {
            string _s = ln.Trim();
            string s = _s.Substring(_s.IndexOf(' ') + 1);

            int i = s.IndexOf("(", StringComparison.OrdinalIgnoreCase);
            string sub = s.Substring(i + 1);
            int c = sub.LastIndexOf(')');
            if (c == -1) { Console.Error.WriteLine("The line \"" + s + "\" is missing a closing parenthesis!"); return; }
            string _i = sub.Substring(0, c); // args

            string[] fields = _i.Split(',');
            if(fields.Length == 0)
            {
                Console.Error.WriteLine("\nThe line \n\"{0}\"\n doesn't have any parameters! Structures must have parameters to be valid.\n", ln);
                return;
            }
            for(int x = 0; x < fields.Length; x++)
                fields[x] = fields[x].Trim();

            string structname = ln.Split(' ')[1].Remove(i);
            struct_names.Add(structname);

            function_buffer.Append("public struct " + structname + " {\n");
            foreach(string field in fields)
            {
                function_buffer.Append("    public dynamic " + field + ";\n");
            }
            function_buffer.Append("    public " + structname + "(");
            foreach (string field in fields)
            {
                if(field.Equals(fields.Last()))
                {
                    function_buffer.Append("dynamic " + field + ") {\n");
                } else
                {
                    function_buffer.Append("dynamic " + field + ", ");
                }
            }
            foreach (string field in fields)
            {
                function_buffer.Append("            this." + field + " = " + field + ";\n");
            }
            function_buffer.Append("    }\n}");
            string fin = function_buffer.ToString();
            function_buffer.Clear();
            function_list.Add(fin);
            return;
        }
        private static void CountCurlyBrackets(string ln)
        {
            int op, cls;
            op = ln.Count(c => c.Equals('{'));
            cls = ln.Count(c => c.Equals('}'));
            int dif = op - cls;
            cur_scope += dif;
        }
        // -----------------------------------------------------------
        public static void TryAddRequiredMethods(string allcode)
        {
            string low = allcode.ToLower();
            if(low.Contains("tryinputkey"))
            {
                _AddLine(tryinputkey);
                Console.WriteLine("Referenced TryInputKey method.");
            }
            if (low.Contains("iskeydown"))
            {
                _AddLine(iskeydown);
                Console.WriteLine("Referenced IsKeyDown method.");
            }
            if (low.Contains("charat"))
            {
                _AddLine(characterat);
                Console.WriteLine("Referenced CharAt method.");
            }
            if (low.Contains("getcenter"))
            {
                _AddLine(fetchcenter);
                Console.WriteLine("Referenced FetchCenter method.");
            }
            if (low.Contains("round("))
            {
                _AddLine(roundint);
                Console.WriteLine("Referenced RoundInt method.");
            }
        }
        public static void Init(string allcode)
        {
            sb = new StringBuilder();
            function_buffer = new StringBuilder();
            function_list = new List<string>();
            function_names = new List<string>();
            struct_names = new List<string>();
            _AddLine("// DO NOT USE THIS CODE AS AN EXAMPLE TO LEARN C#.\n" +
                "// What you're seeing is auto generated code by the simplesharp interpreter.\n" +
                "// The uses of dynamic keywords should be avoided in normal coding because it's much, much, slower than traditional explicitly stated types.");
            _AddLine("using System;");
            _AddLine("using System.Collections.Generic;");
            _AddLine("using System.Diagnostics;");
            _AddLine("using System.Text;");
            _AddLine("using System.Reflection;");
            _AddLine("using System.IO;");
            _AddLine("using System.Linq;");
            _AddLine("using System.Threading;");
            _AddLine("using static System.Math;");
            _AddLine("using Microsoft.CSharp.RuntimeBinder;");
            _AddLine("using SimpleSharp;");
            _AddLine("namespace SimpleSharpCompiled {");
            _AddLine("    class RunCompiled {");
            TryAddRequiredMethods(allcode);
            if (!IS_EXE_OUTPUT())
                _AddLine("        public static void Main(Action _exitDelegate_, Action<int> _threadSleep_) {\n");
            else
                _AddLine("        public static void Main(string[] args) {\n");
        }
        public static void AddLine(string line)
        {
            CountCurlyBrackets(line);
            Console.WriteLine("(SCOPE:{0}) Converting: {1}", cur_scope, line);

            if (cur_scope == 0 && function_declaration == true)
            {
                // Function declaration ended
                function_declaration = false;

                function_buffer.Append("\nreturn null;}");

                string func = function_buffer.ToString();
                function_buffer.Clear();
                function_list.Add(func);
                return;
            }

            string trm = line.Trim().ToLower();
            if (trm.Contains("function ") && !function_declaration)
            {
                _FunctionDeclaration(line);
                return;
            }
            else if (trm.Contains("structure ") && !function_declaration)
            {
                _StructureDeclaration(line);
                return;
            }
            else if(trm.Equals("return"))
            {
                line = line.Replace("return", "return null;",
                    StringComparison.OrdinalIgnoreCase);
            }

            line = _PreStatementAliases(line);
            if(line == null) { return; }

            // Conversions to be made
            if (line.Contains("="))
            {
                if(!InParenthesis(line, line.IndexOf('=')))
                {
                    if(!line.Contains("+=") &&
                    !line.Contains("-=") &&
                    !line.Contains("*=") &&
                    !line.Contains("/="))
                    {
                        line = _ConvertAssignment(line);
                    }
                } else
                {
                    if(line.ToLower().Contains("if(") &&
                    !line.Contains("==") && !line.Contains("!=") &&
                    !line.Contains(">=") && !line.Contains("<="))
                        line = line.Replace("=", "==");
                }
            }
            
            // Aliases
            line = _StatementAliases(line);

            // All non-semicolon operators.
            if (!line.EndsWith(';')
            && !line.EndsWith('{')
            && !line.EndsWith('}')
            && !string.IsNullOrWhiteSpace(line))
            {
                line += ";";
            }
            if(function_declaration)
            {
                function_buffer.Append("            " + line + "\n");
                return;
            }
            _AddLine("            " + line);
        }
        public static void PrepareCompile()
        {
            _AddLine("        }");
            Console.WriteLine("Custom function count: {0}", function_list.Count);
            AppendAllFunctions();
            _AddLine("    }");
            _AddLine("}");

            foreach(Assembly asm in refs)
            {
                // only iterate namespaces
                foreach(string nsp in asm.GetTypes()
                    .Select(type=>type.Namespace)
                    .Distinct())
                {
                    sb.Insert(0, "using " + nsp + ";\n");
                }
            }
        }
        public static void AppendAllFunctions()
        {
            foreach(string func in function_list)
            {
                _AddLine(func);
            }
        }
        public static CSharpCompilation RemapDynamicVariables(CSharpCompilation cs)
        {
            var sem = cs.GetSemanticModel(systree, true);
            var node = systree.GetRoot();

            var vars = node.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .ToArray();
            for(int i = 0; i < vars.Count(); i++)
            {
                LocalDeclarationStatementSyntax lds = vars[i];
                VariableDeclaratorSyntax vds = lds.Declaration.Variables.First();

                Microsoft.CodeAnalysis.TypeInfo initInformation
                    = sem.GetTypeInfo(vds.Initializer.Value);

                var varType = lds.Declaration.Type;
                var _returnType = SyntaxFactory.IdentifierName
                    (initInformation.Type.Name + " "); // For the sb.replace
                var returnType = SyntaxFactory.IdentifierName
                    (initInformation.Type.Name);

                if(initInformation.Type.TypeKind == TypeKind.Array)
                {continue;} else if(initInformation.Type.Name.Equals("List"))
                {continue;}

                var _newDec = lds.ReplaceNode(varType, _returnType); // For the sb.replace

                var newDec = lds.ReplaceNode(varType, returnType);
                var newRt = node.ReplaceNode(lds, newDec);

                string base1 = lds.ToFullString().Replace("\n", "").Trim();
                string base2 = _newDec.ToFullString().Replace("\n", "").Trim();
                sb = sb.Replace(base1, base2);

                node = newRt;

                SyntaxTree oldTree = cs.SyntaxTrees[0];
                SyntaxTree newTree = node.SyntaxTree;
                cs = cs.ReplaceSyntaxTree(oldTree,newTree);
                sem = cs.GetSemanticModel(newTree);
                vars = node.DescendantNodes()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .ToArray();
            }
            var newsys = systree.WithRootAndOptions(node, new CSharpParseOptions());
            systree = newsys;

            Program.WriteCode(sb.ToString());

            return CSharpCompilation.Create(
                cs.AssemblyName, new[] { systree },
                cs.References, cs.Options);
        }
        public static void Compile(string name)
        {
            Console.WriteLine("Parsing source code into SyntaxTree...");
            systree = CSharpSyntaxTree.ParseText(sb.ToString());
            Thread.Sleep(1000);

            Console.WriteLine("Getting assembly paths...");
            List<string> refPaths = ReferenceManager.assemblyPaths.ToList();

            // add custom references
            foreach(Assembly asm in refs)
            {
                refPaths.Add(asm.Location);
            }

            foreach(string pth in refPaths)
            {
                Console.WriteLine("Recognized build assembly: " + pth);
            }
            List<PortableExecutableReference> references = refPaths.Select
                (r => MetadataReference.CreateFromFile(r)).ToList();
            foreach (PortableExecutableReference per in references)
            {
                Console.WriteLine("Created mdreference: " + per.Display);
            }
            //string cspath = Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location;
            //Console.WriteLine(cspath);
            //references.Add(MetadataReference.CreateFromFile(cspath));

            Console.WriteLine("Building CSCompilation...");
            // 0 = Default
            // 1 = DLL
            // 2 = EXE
            // 3 = CODE
            if(OUTPUT_TYPE == 0
            || OUTPUT_TYPE == 3)
            {
                SubCompileDefault(name, references);
            } else if(OUTPUT_TYPE == 1) {
                SubCompileDLL(name + ".dll", references);
            } else if (OUTPUT_TYPE == 2) {
                SubCompileEXE(name + ".exe", references);
            }
        }
        public static void SubCompileDefault(string name, IEnumerable<MetadataReference> refs)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                syntaxTrees: new[] { systree },
                references: refs,
                options: options);

            compilation = RemapDynamicVariables(compilation);

            Console.WriteLine("Built CSCompilation...");
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                Console.WriteLine("Emitted compilation IL.");
                if (!result.Success)
                {
                    Console.WriteLine("INTERNAL ERROR!");
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    DisplayErrors(failures);
                    return;
                }
                else
                {
                    Console.WriteLine("Reading output library...");
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    Console.WriteLine("Read output library.");
                    var type = assembly.GetType("SimpleSharpCompiled.RunCompiled");
                    Console.WriteLine("Fetched target namespace.");
                    var instance = assembly.CreateInstance("SimpleSharpCompiled.RunCompiled");
                    Console.WriteLine("Fetched target class.");
                    var methd = type.GetMethod("Main") as MethodInfo;
                    Console.WriteLine("Fetched main method.");
                    Console.Clear();

                    try
                    {
                        methd.Invoke(instance, new Delegate[]  {
                        new Action(Exit),
                        new Action<int>(ThreadSleep)});
                    } catch(Exception exc)
                    {
                        string msg = exc.Message;

                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Uh oh, an error occurred in your code!\n" +
                            "Let's try and figure out what the problem is.\n\n");
                        Console.WriteLine("This is the error it gave you:\n\"{0}\"", msg);
                        Console.ReadLine();
                    }
                }
            }
        }
        public static void SubCompileDLL(string name, IEnumerable<MetadataReference> refs)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                syntaxTrees: new[] { systree },
                references: refs,
                options: options);
            Console.WriteLine("Built CSCompilation...");
            EmitResult result = compilation.Emit(name);
            Console.WriteLine("Emitted compilation IL.");
            if (!result.Success)
            {
                Console.WriteLine("INTERNAL ERROR!");
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);
                DisplayErrors(failures);
                return;
            }
            Console.WriteLine("Successfully output dll file to: " + name);
        }
        public static void SubCompileEXE(string name, IEnumerable<MetadataReference> refs)
        {
            string rawname = name.Remove(name.Length - 4);
            string _fp = Program.PATH;
            string directory = _fp.Remove(_fp.LastIndexOf('\\') + 1);
            string fullpath = directory + name;
            string fullpathexe = directory + rawname + ".exe";

            var options = new CSharpCompilationOptions(OutputKind.WindowsApplication)
                .WithMetadataImportOptions(MetadataImportOptions.All)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu);

            // Force BinderFlags.IgnoreAccessibility on the options instance.
            //PropertyInfo TLBinderFlagsField = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            //TLBinderFlagsField.SetValue(options, (uint)1 << 22);

            CSharpCompilation compilation = CSharpCompilation.Create("SimpleSharpInternals",
                syntaxTrees: new[] { systree },
                references: refs,
                options: options);
            Console.WriteLine("Built CSCompilation...");

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                Console.WriteLine("Emitted compilation IL.");
                if (!result.Success)
                {
                    Console.WriteLine("INTERNAL ERROR!");
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    DisplayErrors(failures);
                    return;
                }
                else
                {
                    Console.WriteLine("Reading output library...");
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    Console.WriteLine("Read output library.");
                    var type = assembly.GetType("SimpleSharpCompiled.RunCompiled");
                    Console.WriteLine("Fetched target namespace.");
                    var instance = assembly.CreateInstance("SimpleSharpCompiled.RunCompiled");
                    Console.WriteLine("Fetched target class.");
                    var methd = type.GetMethod("Main") as MethodInfo;
                    Console.WriteLine("Fetched main method.");
                    Thread.Sleep(350); // so i can actually see it
                    Console.Clear();

                    methd.Invoke(instance, new Delegate[]  {
                        new Action(Exit),
                        new Action<int>(ThreadSleep)});
                }
            }
        }
        public static void CompileWithDebugger(string name)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Compile(name);
            sw.Stop();
            ConsoleColor bf = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Running finished. Total time: " + sw.ElapsedMilliseconds / 1000 + "s");
            Console.ForegroundColor = bf;
        }
        private static void DisplayErrors(IEnumerable<Diagnostic> errs)
        {
            foreach (Diagnostic diagnostic in errs)
            {
                Console.Error.WriteLine("\tL{0}: {1}", diagnostic.Location.SourceSpan.ToString(), diagnostic.GetMessage());
            }
            Console.WriteLine("\nC# Code:\n" + sb.ToString());
            Console.ReadLine();
            Environment.Exit(0);
            return;
        }
        private static string GetParenthesisContents(string ln, string fname, out int ind)
        {
            int i = ln.IndexOf(fname + "(", StringComparison.OrdinalIgnoreCase);
            int index = i + fname.Length;
            ind = i;
            int closer = FindClosingParenthesis(ln, index);
            string sub1 = ln.Substring(index+1);
            string sub2 = sub1.Substring(0, closer - (index+1));
            return sub2;
        }
        internal static void FillArray<T>(T[] a, T[] b)
        {
            Array.Copy(b, a, b.Length);

            int fhl = a.Length / 2;

            for (int i = b.Length; i < a.Length; i *= 2)
            {
                int cpl = i;
                if (i > fhl)
                {
                    cpl = a.Length - i;
                }

                Array.Copy(a, 0, a, i, cpl);
            }
        }

        // Ext methods
        public static void Exit()
        {
            Environment.Exit(0);
        }
        public static void ThreadSleep(int millis)
        {
            // flush pending console keys
            while(Console.KeyAvailable)
                Console.ReadKey(true);

            // actually sleep
            Thread.Sleep(millis);
        }
    }
}