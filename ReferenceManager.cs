using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SimpleSharp
{
    public static class ReferenceManager
    {
        internal static bool valid = false;

        public static Assembly[] assemblies;
        public static string[] assemblyPaths;

        public static string[] referencedMethods;
        public static string[] referencedFields;
        public static string[] referencedClasses;
        public static void Enable()
        {
            assemblies = new List<Assembly> {
                typeof(object).GetTypeInfo().Assembly,
                typeof(Console).GetTypeInfo().Assembly,
                typeof(Enumerable).GetTypeInfo().Assembly,
                typeof(CollectionExtensions).GetTypeInfo().Assembly,
                typeof(File).GetTypeInfo().Assembly,
                typeof(Assembly).GetTypeInfo().Assembly,
                typeof(CSharpArgumentInfo).GetTypeInfo().Assembly,
                typeof(CSharpSyntaxTree).GetTypeInfo().Assembly,
                typeof(System.Dynamic.CallInfo).GetTypeInfo().Assembly,
                typeof(RuntimeBinderException).GetTypeInfo().Assembly,
                typeof(System.Dynamic.ConvertBinder).GetTypeInfo().Assembly,
                typeof(ExpressionType).GetTypeInfo().Assembly,
                typeof(CodeConsumer).GetTypeInfo().Assembly,
                Assembly.Load(new AssemblyName("Microsoft.CSharp")),
                Assembly.Load(new AssemblyName("netstandard")),
                Assembly.Load(new AssemblyName("System.Dynamic.Runtime")),
                Assembly.Load(new AssemblyName("System.Runtime")),
                Assembly.Load(new AssemblyName("mscorlib"))
            }.Distinct().ToArray();

            assemblyPaths = new string[assemblies.Length];
            for(int x = 0; x < assemblies.Length; x++)
            {
                assemblyPaths[x] = assemblies[x].Location;
            }

            List<MethodInfo> _methods = new List<MethodInfo>();
            List<FieldInfo> _fields = new List<FieldInfo>();
            List<string> _types = new List<string>();
            List<string> methods = new List<string>();
            List<string> fields = new List<string>();
            foreach (Assembly asm in assemblies)
            {
                foreach(Type tp in asm.GetTypes())
                {
                    _types.Add(tp.Name);
                    _methods.AddRange(tp.GetMethods());
                    _fields.AddRange(tp.GetFields());
                }
            }

            foreach(MethodInfo inf in _methods)
            {
                methods.Add(inf.Name);
            }
            foreach (FieldInfo inf in _fields)
            {
                fields.Add(inf.Name);
            }
            referencedMethods = methods.ToArray();
            referencedFields = fields.ToArray();
            referencedClasses = _types.ToArray();
        }
    }
}
