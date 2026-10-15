// 用反射导出 GodotSharp / GodotSharpEditor 程序集的公开 C# API，输出 JSON。
// 用法：dotnet run -- <GodotSharp.dll> <GodotSharpEditor.dll> <out.json>
// 每个成员同时给出 XML 文档注释的成员 ID，供 gen_api_docs.py 对上 GodotSharp.xml 里的说明。
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

static class Program
{
    const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: ApiDump <GodotSharp.dll> <GodotSharpEditor.dll> <out.json>");
            return 2;
        }
        var dir = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var p = Path.Combine(dir, name.Name + ".dll");
            return File.Exists(p) ? ctx.LoadFromAssemblyPath(p) : null;
        };
        var asms = new[] { args[0], args[1] }.Select(p => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(p))).ToArray();
        var types = new List<object>();
        foreach (var asm in asms)
        {
            Type[] all;
            try { all = asm.GetExportedTypes(); }
            catch (ReflectionTypeLoadException e) { all = e.Types.Where(t => t != null).ToArray()!; }
            foreach (var t in all.OrderBy(t => t.FullName, StringComparer.Ordinal))
                types.Add(DumpType(t, asm.GetName().Name!));
        }
        var json = JsonSerializer.Serialize(new { assemblies = asms.Select(a => a.GetName().ToString()), types },
            new JsonSerializerOptions { WriteIndented = false });
        File.WriteAllText(args[2], json, new UTF8Encoding(false));
        Console.WriteLine($"types: {types.Count}");
        return 0;
    }

    static object DumpType(Type t, string asm)
    {
        string kind = t.IsEnum ? "enum" : t.IsInterface ? "interface" : t.IsValueType ? "struct"
            : typeof(Delegate).IsAssignableFrom(t) ? "delegate" : (t.IsAbstract && t.IsSealed) ? "static class" : "class";
        var members = new List<object>();
        if (t.IsEnum)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                members.Add(new { kind = "enumValue", name = f.Name, sig = $"{f.Name} = {Convert.ToInt64(f.GetRawConstantValue())}", docId = "F:" + DocTypeName(t) + "." + f.Name, isStatic = true });
        }
        else if (kind == "delegate")
        {
            var inv = t.GetMethod("Invoke")!;
            members.Add(new { kind = "invoke", name = "Invoke", sig = $"delegate {TypeName(inv.ReturnType)} {Clean(t.Name)}({Params(inv)})", docId = "", isStatic = false });
        }
        else
        {
            foreach (var c in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                members.Add(new { kind = "ctor", name = Clean(t.Name), sig = $"public {Clean(t.Name)}({Params(c)})", docId = "M:" + DocTypeName(t) + ".#ctor" + DocParams(c), isStatic = false });
            foreach (var p in t.GetProperties(Declared))
            {
                var acc = p.GetGetMethod() ?? p.GetSetMethod();
                if (acc == null) continue;
                var idx = p.GetIndexParameters();
                string accs = (p.GetGetMethod() != null ? "get; " : "") + (p.GetSetMethod() != null ? "set; " : "");
                string pname = idx.Length > 0 ? $"this[{string.Join(", ", idx.Select(Param))}]" : p.Name;
                members.Add(new
                {
                    kind = "property", name = p.Name,
                    sig = $"public {(acc.IsStatic ? "static " : "")}{TypeName(p.PropertyType)} {pname} {{ {accs}}}",
                    docId = "P:" + DocTypeName(t) + "." + p.Name + (idx.Length > 0 ? "(" + string.Join(",", idx.Select(i => DocType(i.ParameterType))) + ")" : ""),
                    isStatic = acc.IsStatic
                });
            }
            foreach (var m in t.GetMethods(Declared))
            {
                if (m.IsSpecialName && !m.Name.StartsWith("op_")) continue;
                if (m.GetCustomAttribute<CompilerGeneratedAttribute>() != null) continue;
                string gen = m.IsGenericMethodDefinition ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";
                string mods = (m.IsStatic ? "static " : "") + (m.IsAbstract && !t.IsInterface ? "abstract " : m.IsVirtual && !m.IsFinal && !t.IsInterface ? "virtual " : "");
                members.Add(new
                {
                    kind = m.Name.StartsWith("op_") ? "operator" : "method", name = m.Name,
                    sig = $"public {mods}{TypeName(m.ReturnType)} {m.Name}{gen}({Params(m)})",
                    docId = "M:" + DocTypeName(t) + "." + m.Name + (m.IsGenericMethodDefinition ? "``" + m.GetGenericArguments().Length : "") + DocParams(m)
                        + (m.Name is "op_Implicit" or "op_Explicit" ? "~" + DocType(m.ReturnType) : ""),
                    isStatic = m.IsStatic
                });
            }
            foreach (var e in t.GetEvents(Declared))
                members.Add(new { kind = "event", name = e.Name, sig = $"public event {TypeName(e.EventHandlerType!)} {e.Name}", docId = "E:" + DocTypeName(t) + "." + e.Name, isStatic = false });
            foreach (var f in t.GetFields(Declared))
            {
                if (f.IsSpecialName) continue;
                string val = f.IsLiteral ? " = " + FormatConst(f.GetRawConstantValue()) : "";
                string mods = f.IsLiteral ? "const " : (f.IsStatic ? "static " : "") + (f.IsInitOnly ? "readonly " : "");
                members.Add(new { kind = f.IsLiteral ? "constant" : "field", name = f.Name, sig = $"public {mods}{TypeName(f.FieldType)} {f.Name}{val}", docId = "F:" + DocTypeName(t) + "." + f.Name, isStatic = f.IsStatic });
            }
        }
        return new
        {
            assembly = asm,
            ns = t.Namespace,
            name = DisplayName(t),
            fullName = DocTypeName(t),
            declaringType = t.DeclaringType != null ? DocTypeName(t.DeclaringType) : null,
            kind,
            baseType = t.BaseType != null && t.BaseType != typeof(object) && t.BaseType != typeof(ValueType) && t.BaseType != typeof(Enum) && t.BaseType != typeof(MulticastDelegate) ? TypeName(t.BaseType) : null,
            interfaces = t.GetInterfaces().Select(TypeName).OrderBy(s => s).ToArray(),
            isAbstract = t.IsAbstract && !t.IsSealed,
            isSealed = t.IsSealed && !t.IsAbstract,
            docId = "T:" + DocTypeName(t),
            members,
        };
    }

    static string FormatConst(object? v) => v switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        bool b => b ? "true" : "false",
        float f => f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f",
        double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };

    static string Clean(string n) { int i = n.IndexOf('`'); return i < 0 ? n : n[..i]; }

    static string DisplayName(Type t)
    {
        var n = Clean(t.Name);
        if (t.IsGenericTypeDefinition) n += "<" + string.Join(", ", t.GetGenericArguments().Select(a => a.Name)) + ">";
        return t.DeclaringType != null ? DisplayName(t.DeclaringType) + "." + n : n;
    }

    static readonly Dictionary<Type, string> Alias = new()
    {
        [typeof(void)] = "void", [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint",
        [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double",
        [typeof(string)] = "string", [typeof(object)] = "object", [typeof(char)] = "char", [typeof(decimal)] = "decimal",
    };

    static string TypeName(Type t)
    {
        if (t.IsByRef) return TypeName(t.GetElementType()!);
        if (Alias.TryGetValue(t, out var a)) return a;
        if (t.IsArray) return TypeName(t.GetElementType()!) + "[" + new string(',', t.GetArrayRank() - 1) + "]";
        if (t.IsPointer) return TypeName(t.GetElementType()!) + "*";
        if (t.IsGenericParameter) return t.Name;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) return TypeName(t.GetGenericArguments()[0]) + "?";
        string n = Clean(t.Name);
        if (t.IsGenericType) n += "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">";
        if (t.IsNested && !t.IsGenericParameter) n = TypeName(t.DeclaringType!.IsGenericTypeDefinition ? t.DeclaringType : t.DeclaringType) + "." + n;
        return n;
    }

    static string Param(ParameterInfo p)
    {
        string pre = p.ParameterType.IsByRef ? (p.IsOut ? "out " : p.IsIn ? "in " : "ref ") : "";
        if (p.GetCustomAttribute<ParamArrayAttribute>() != null) pre = "params ";
        string def = "";
        if (p.HasDefaultValue)
        {
            var v = p.RawDefaultValue;
            def = " = " + (v == null ? (p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) == null ? "default" : "null")
                : p.ParameterType.IsEnum || (Nullable.GetUnderlyingType(p.ParameterType)?.IsEnum ?? false)
                    ? TypeName(Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType) + "." + Enum.GetName(Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType, v)
                    : FormatConst(v));
        }
        return $"{pre}{TypeName(p.ParameterType)} {p.Name}{def}";
    }

    static string Params(MethodBase m) => string.Join(", ", m.GetParameters().Select(Param));

    // ---- XML 文档注释的成员 ID（ECMA-334 附录 D） ----
    static string DocTypeName(Type t)
    {
        if (t.IsNested) return DocTypeName(t.DeclaringType!) + "." + t.Name;
        return (t.Namespace != null ? t.Namespace + "." : "") + t.Name;
    }

    static string DocParams(MethodBase m)
    {
        var ps = m.GetParameters();
        return ps.Length == 0 ? "" : "(" + string.Join(",", ps.Select(p => DocType(p.ParameterType))) + ")";
    }

    static string DocType(Type t)
    {
        if (t.IsByRef) return DocType(t.GetElementType()!) + "@";
        if (t.IsPointer) return DocType(t.GetElementType()!) + "*";
        if (t.IsArray) return DocType(t.GetElementType()!) + (t.GetArrayRank() == 1 ? "[]" : "[" + string.Join(",", Enumerable.Repeat("0:", t.GetArrayRank())) + "]");
        if (t.IsGenericParameter) return (t.DeclaringMethod != null ? "``" : "`") + t.GenericParameterPosition;
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            string baseName = DocTypeName(def);
            baseName = baseName[..baseName.LastIndexOf('`')];
            return baseName + "{" + string.Join(",", t.GetGenericArguments().Select(DocType)) + "}";
        }
        return DocTypeName(t);
    }
}
