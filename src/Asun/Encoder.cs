using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Asun;

/// <summary>
/// High-performance ASUN text encoder with schema header caching.
/// </summary>
public static class Encoder
{
    // Cache schema headers per type to avoid rebuilding identical headers
    private static readonly ConcurrentDictionary<Type, string> _headerCache = new();
    private static readonly ConcurrentDictionary<Type, string> _headerTypedCache = new();

    // Thread-local writer for single-struct encode — avoids repeated ArrayPool rent/return
    [ThreadStatic] private static AsunWriter t_writer;

    public static string Encode(IAsunSchema value)
    {
        var header = GetOrBuildHeader(value, false);
        ref var w = ref t_writer;
        if (w.IsEmpty) w = new AsunWriter(256);
        else w.Reset();

        w.WriteSpan(header);
        w.WriteChar('(');
        value.WriteValues(ref w);
        w.WriteChar(')');
        return w.ToString();
    }

    public static string Encode<T>(IReadOnlyList<T> values) where T : IAsunSchema
    {
        if (values.Count == 0) return "[]";
        var header = GetOrBuildListHeader(values[0], false);
        int cap = header.Length + values.Count * 64;
        var w = new AsunWriter(cap);
        try
        {
            w.WriteSpan(header);
            for (int r = 0; r < values.Count; r++)
            {
                if (r > 0) w.WriteChar(',');
                w.WriteChar('(');
                values[r].WriteValues(ref w);
                w.WriteChar(')');
            }
            return w.ToString();
        }
        finally { w.Dispose(); }
    }

    public static string EncodeTyped(IAsunSchema value)
    {
        var header = GetOrBuildHeader(value, true);
        ref var w = ref t_writer;
        if (w.IsEmpty) w = new AsunWriter(256);
        else w.Reset();

        w.WriteSpan(header);
        w.WriteChar('(');
        value.WriteValues(ref w);
        w.WriteChar(')');
        return w.ToString();
    }

    public static string EncodeTyped<T>(IReadOnlyList<T> values) where T : IAsunSchema
    {
        if (values.Count == 0) return "[]";
        var header = GetOrBuildListHeader(values[0], true);
        int cap = header.Length + values.Count * 64;
        var w = new AsunWriter(cap);
        try
        {
            w.WriteSpan(header);
            for (int r = 0; r < values.Count; r++)
            {
                if (r > 0) w.WriteChar(',');
                w.WriteChar('(');
                values[r].WriteValues(ref w);
                w.WriteChar(')');
            }
            return w.ToString();
        }
        finally { w.Dispose(); }
    }

    // Build and cache schema header string: "{field1,field2,...}:" or "[{field1,field2,...}]:"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetOrBuildHeader(IAsunSchema obj, bool typed)
    {
        var cache = typed ? _headerTypedCache : _headerCache;
        var type = obj.GetType();
        if (cache.TryGetValue(type, out var cached)) return cached;
        var header = BuildStructHeader(obj, typed);
        cache.TryAdd(type, header);
        return header;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetOrBuildListHeader(IAsunSchema first, bool typed)
    {
        // For list, we use a different key prefix. Reuse same cache with a wrapper type trick.
        // Actually just build it from the struct header
        var structHeader = GetOrBuildHeader(first, typed);
        // structHeader is like "{id,name,...}:" → need "[{id,name,...}]:"
        // remove trailing ":" and wrap with "[" "]:"
        return "[" + structHeader[..^1] + "]:";
    }

    private static string BuildStructHeader(IAsunSchema obj, bool typed)
    {
        var w = new AsunWriter(128);
        try
        {
            WriteSchemaHeader(ref w, obj, typed);
            w.WriteSpan("}:");
            return w.ToString();
        }
        finally { w.Dispose(); }
    }

    private static void WriteSchemaHeader(ref AsunWriter w, IAsunSchema obj, bool typed)
    {
        var names = obj.FieldNames;
        var types = obj.FieldTypes;
        var values = obj.FieldValues; // only for nested type detection in header build

        w.WriteChar('{');
        for (int i = 0; i < names.Length; i++)
        {
            if (i > 0) w.WriteChar(',');
            WriteSchemaFieldName(ref w, names[i]);

            var v = i < values.Length ? values[i] : null;
            if (v is System.Collections.IDictionary) throw AsunException.UnsupportedMap;
            if (v is IAsunSchema nested)
            {
                w.WriteChar('@');
                WriteNestedSchemaHeader(ref w, nested, typed);
            }
            else if (v is System.Collections.IList list && list.Count > 0 && list[0] is IAsunSchema firstSchema)
            {
                w.WriteSpan("@[");
                WriteNestedSchemaHeader(ref w, firstSchema, typed);
                w.WriteChar(']');
            }
            else if (v is System.Collections.IList scalarList)
            {
                WriteArrayTypeHeader(ref w, scalarList, i < types.Length ? types[i] : null, typed);
            }
            else if (i < types.Length && types[i] is string fieldType)
            {
                WriteDeclaredTypeHeader(ref w, fieldType, typed);
            }
        }
    }

    private static void WriteNestedSchemaHeader(ref AsunWriter w, IAsunSchema obj, bool typed)
    {
        var names = obj.FieldNames;
        var types = obj.FieldTypes;
        var values = obj.FieldValues;

        w.WriteChar('{');
        for (int i = 0; i < names.Length; i++)
        {
            if (i > 0) w.WriteChar(',');
            WriteSchemaFieldName(ref w, names[i]);

            var v = i < values.Length ? values[i] : null;
            if (v is System.Collections.IDictionary) throw AsunException.UnsupportedMap;
            if (v is IAsunSchema nested)
            {
                w.WriteChar('@');
                WriteNestedSchemaHeader(ref w, nested, typed);
            }
            else if (v is System.Collections.IList scalarList)
            {
                WriteArrayTypeHeader(ref w, scalarList, i < types.Length ? types[i] : null, typed);
            }
            else if (i < types.Length && types[i] is string fieldType)
            {
                WriteDeclaredTypeHeader(ref w, fieldType, typed);
            }
        }
        w.WriteChar('}');
    }

    private static void WriteSchemaFieldName(ref AsunWriter w, string name)
    {
        if (!SchemaFieldNameNeedsQuoting(name))
        {
            w.WriteSpan(name);
            return;
        }
        w.WriteChar('"');
        for (int i = 0; i < name.Length; i++)
        {
            switch (name[i])
            {
                case '"': w.WriteSpan("\\\""); break;
                case '\\': w.WriteSpan("\\\\"); break;
                case '\n': w.WriteSpan("\\n"); break;
                case '\r': w.WriteSpan("\\r"); break;
                case '\t': w.WriteSpan("\\t"); break;
                case '\b': w.WriteSpan("\\b"); break;
                case '\f': w.WriteSpan("\\f"); break;
                default: w.WriteChar(name[i]); break;
            }
        }
        w.WriteChar('"');
    }

    private static bool SchemaFieldNameNeedsQuoting(string name)
    {
        if (name.Length == 0) return true;
        if (name == "true" || name == "false") return true;
        if (name[0] == ' ' || name[^1] == ' ') return true;
        bool couldBeNumber = true;
        int numStart = name[0] == '-' ? 1 : 0;
        if (numStart >= name.Length) couldBeNumber = false;
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsWhiteSpace(c) || c is ',' or '@' or ':' or '{' or '}' or '[' or ']' or '(' or ')' or '"' or '\\')
                return true;
            if (couldBeNumber && i >= numStart && !((c >= '0' && c <= '9') || c == '.'))
                couldBeNumber = false;
        }
        return couldBeNumber && name.Length > numStart;
    }

    private static void WriteDeclaredTypeHeader(ref AsunWriter w, string fieldType, bool typed)
    {
        if (fieldType.Length == 0) return;
        bool isComplex = fieldType[0] == '{' || fieldType[0] == '[';
        if (!typed && !isComplex) return;
        w.WriteChar('@');
        w.WriteSpan(fieldType);
    }

    private static void WriteArrayTypeHeader(ref AsunWriter w, System.Collections.IList list, string? fieldType, bool typed)
    {
        w.WriteSpan("@[");

        if (list.Count > 0)
        {
            if (list[0] is System.Collections.IDictionary) throw AsunException.UnsupportedMap;
            if (list[0] is not IAsunSchema && typed)
            {
                var elemType = InferScalarType(list[0]);
                if (elemType is not null) w.WriteSpan(elemType);
            }
        }
        else if (fieldType is not null)
        {
            if (fieldType.StartsWith('[') && fieldType.EndsWith(']'))
            {
                w.WriteSpan(fieldType.AsSpan(1, fieldType.Length - 2));
            }
            else if (typed)
            {
                w.WriteSpan(fieldType);
            }
        }

        w.WriteChar(']');
    }

    private static string? InferScalarType(object? value)
    {
        return value switch
        {
            bool => AsunType.Bool,
            int or long => AsunType.Int,
            float or double => AsunType.Float,
            string => AsunType.Str,
            _ => null,
        };
    }

    // Legacy entry points for internal use (PrettyPrinter etc.)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EncodeStruct(ref AsunWriter w, IAsunSchema obj, bool typed)
    {
        var header = GetOrBuildHeader(obj, typed);
        w.WriteSpan(header);
        w.WriteChar('(');
        obj.WriteValues(ref w);
        w.WriteChar(')');
    }

    internal static void EncodeTopList<T>(ref AsunWriter w, IReadOnlyList<T> list, bool typed) where T : IAsunSchema
    {
        if (list.Count == 0) { w.WriteSpan("[]"); return; }
        var header = GetOrBuildListHeader(list[0], typed);
        w.WriteSpan(header);
        for (int r = 0; r < list.Count; r++)
        {
            if (r > 0) w.WriteChar(',');
            w.WriteChar('(');
            list[r].WriteValues(ref w);
            w.WriteChar(')');
        }
    }
}
