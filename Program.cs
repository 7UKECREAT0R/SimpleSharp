using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleSharp
{
    // What I want to do:
    /*
     * Store reference assemblies. ✔
     * Store reference urls. ✔
     * Store their methods. ✔
     * Store their fields. ✔
     * Scan over each of them on each line to enable full in-case-sensitivity. ✔
     * 
     * Change dynamic keywords to their specified variable return types for major speed improvements. ✔
    */
    class Program
    {
        public static string PATH;
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("The SimpleSharp Compiler is meant to be run with a file!");
                Console.ReadLine();
                Environment.Exit(0);
                return;
            }
            string path = args[0];
            PATH = path;
            if(!File.Exists(path))
            {
                Console.WriteLine("The specified file to read does not exist. Check if the file name is correct!");
                Console.ReadLine();
                Environment.Exit(0);
                return;
            }
            Console.WriteLine("Reading \"" + path + "\"...");
            string[] lines = File.ReadAllLines(path);
            for(int i = 0; i < lines.Length; i++)
            {
                string trim = lines[i].Trim();
                if (trim.StartsWith('#'))
                {
                    lines[i] = "";
                    CodeConsumer._CompilerMessage(trim);
                }
            }

            InitConversions();
            Console.WriteLine("Initalized Conversions.");

            ReferenceManager.Enable();
            Console.WriteLine("Fetched info of referenced assemblies.");

            string all = string.Join('\n', lines);
            //Console.WriteLine("Initializing Code: " + all);
            CodeConsumer.Init(all); 
            foreach (string ln in lines)
            {
                // Actual line conversion.
                CodeConsumer.AddLine(ln);
            }

            // Check case of custom functions.
            for(int xx = 0; xx < lines.Length; xx++)
            {
                string ln = lines[xx];
                foreach(string func in CodeConsumer.function_names)
                {
                    string a = ln.ToLower();
                    string b = func.ToLower();
                    if (a.Contains(b + "("))
                    {
                        // set it to proper case.
                        lines[xx] = ln.Replace(b, b, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            CodeConsumer.PrepareCompile();
            string[] spl = path.Split('\\');
            string _name = spl[^1].Split('.')[0];
            // _name is the file name without the extension.

            if (CodeConsumer.OUTPUT_TYPE == 3)
            {
                WriteCode(CodeConsumer.sb.ToString());
            }
            
            CodeConsumer.CompileWithDebugger(_name);
            Console.ReadLine();
            Environment.Exit(0);
            return;
        }
        public static void WriteCode(string _code)
        {
            string[] code = _code.Split('\n');
            string namew = Path.GetFileNameWithoutExtension(PATH);
            string pth = namew + ".cs";
            File.WriteAllLines(pth, code);
            Console.WriteLine("Outputted code file: " + pth);
        }
        static void InitConversions()
        {
            Dictionary<string, string> c = new Dictionary<string, string>();
            // Enums
            c.Add("key.", "ConsoleKey.");
            c.Add("consolecolor.", "ConsoleColor.");
            // Functions

            c.Add("break()", "break");
            //c.Add("loop()", "while(true)");

            c.Add("charat(", "__FetchCharAt(");
            c.Add("write(", "Console.Write(");
            c.Add("drawat(", "PInvoke.WriteAt(");
            c.Add("drawrectangle(", "PInvoke.WriteRectangleAt(");
            c.Add("drawline(", "PInvoke.WriteLineAt(");
            c.Add("drawhollowrectangle(", "PInvoke.WriteHollowRectangleAt(");
            c.Add("render()", "PInvoke.RenderWLQUEUE()");
            c.Add("writeline(", "Console.WriteLine(");
            c.Add("setcursorpos(", "Console.SetCursorPosition(");
            c.Add("cursorx", "Console.CursorLeft");
            c.Add("cursory", "Console.CursorTop");
            c.Add("getcenterx()", "__FetchCenter(0)");
            c.Add("getcentery()", "__FetchCenter(1)");
            c.Add("beep(", "Console.Beep(");
            c.Add("input()", "Console.ReadLine()");
            c.Add("pause()", "Console.ReadKey()");
            c.Add("tryinputkey()", "__TryInputK()");
            c.Add("inputkey()", "Console.ReadKey(true)");
            c.Add("iskeydown(", "__IsKD(");
            c.Add("clear()", "PInvoke.Clear()");
            c.Add("if(", "if("); // for case-insensitivity
            c.Add("parseint(", "int.Parse(");
            c.Add("parse(", "double.Parse(");
            c.Add("save(", "File.WriteAllText(");
            c.Add("load(", "File.ReadAllText(");
            c.Add("exists(", "File.Exists(");
            c.Add("delete(", "File.Delete(");
            c.Add("setconsoleselectable(", "PInvoke.SetQuickEditMode(");
            c.Add("presskeys(", "PInvoke.SendKeysInternal(");
            c.Add("clickleft(", "PInvoke.SendLeftClickInternal(");
            c.Add("clickright(", "PInvoke.SendRightClickInternal(");
            c.Add("clickmiddle(", "PInvoke.SendMiddleClickInternal(");
            c.Add("getmouseX()", "PInvoke.GetMousePositionOnAxis(0)");
            c.Add("getmouseY()", "PInvoke.GetMousePositionOnAxis(1)");
            c.Add("setmouseposition(", "PInvoke.SetMousePositionInternal(");
            c.Add("mouseevent(", "PInvoke.SendMouseEventInternalFromCommand(");

            // Delegates (using different definitions 
            // for different application types.)
            if (CodeConsumer.IS_EXE_OUTPUT())
            {
                c.Add("exit()", "Environment.Exit(0)");
                c.Add("wait(", "Thread.Sleep(");
                Console.WriteLine("Set EXECUTABLE Conversions.");
            } else
            {
                c.Add("exit()", "_exitDelegate_()");
                c.Add("wait(", "_threadSleep_(");
                Console.WriteLine("Set NON-EXECUTABLE Conversions.");
            }

            // Math
            c.Add("round(", "__roundInt(");
            c.Add("sqrt(", "Sqrt(");
            c.Add("absolute(", "Abs(");
            c.Add("abs(", "Abs(");
            c.Add("sin(", "Sin(");
            c.Add("cos(", "Cos(");
            c.Add("tan(", "Tan(");
            c.Add("hsin(", "Sinh(");
            c.Add("hcos(", "Cosh(");
            c.Add("htan(", "Tanh(");

            c.Add("math.pi", "Math.PI");
            c.Add("math.e", "Math.E");

            // Instance Methods
            c.Add(".type", ".GetType().Name");
            c.Add(".string()", ".ToString()");

            // String
            c.Add(".lowercase()", ".ToString().ToLower()");
            c.Add(".uppercase()", ".ToString().ToUpper()");
            c.Add(".replace(", ".Replace(");
            c.Add(".split(", ".Split(");
            c.Add(".startswith(", ".StartsWith(");
            c.Add(".endswith(", ".EndsWith(");
            c.Add(".positionof(", ".IndexOf(");
            c.Add(".trim(", ".Substring(");
            c.Add(".contains(", ".Contains(");
            c.Add(".length", ".Length");
            c.Add("string.isempty(", "string.IsNullOrWhiteSpace(");
            c.Add("string.empty", "string.Empty");

            // Int
            c.Add("int.max", "int.MaxValue");
            c.Add("int.min", "int.MinValue");
            c.Add("int.parse(", "int.Parse(");

            // Double
            c.Add("double.notanumber", "double.NaN");
            c.Add("double.infinity", "double.PositiveInfinity");
            c.Add("double.negativeinfinity", "double.NegativeInfinity");
            c.Add("double.max", "double.MaxValue");
            c.Add("double.min", "double.MinValue");
            c.Add("double.epsilon", "double.Epsilon");
            c.Add("double.parse(", "double.Parse(");
            c.Add("double.isinfinite(", "double.IsInfinity(");
            c.Add("double.isnotanumber(", "double.IsNaN(");

            // Array
            c.Add(".list()", ".ToList()"); // contains is already added
            c.Add(".reverse()", ".Reverse()");

            // List
            c.Add(".array()", ".ToArray()");
            c.Add(".count", ".Count");
            c.Add(".get(", ".ElementAt(");
            c.Add(".getfirst()", ".First()");
            c.Add(".getlast()", ".Last()");
            c.Add(".add(", ".Add(");
            c.Add(".remove(", ".Remove(");
            c.Add(".removeat(", ".RemoveAt(");
            c.Add(".clear()", ".Clear()");

            c.Add("otherwise", "else");
            c.Add(".equals(", ".Equals(");

            CodeConsumer.conversions = c;
        }
    }
}
