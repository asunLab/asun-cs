// ASON — Array-Schema Object Notation for .NET
using System.Text.Json.Serialization;

namespace Ason;

/// <summary>Schema descriptor for ASON struct serialization.</summary>
public interface IAsonSchema
{
    ReadOnlySpan<string> FieldNames { get; }
    ReadOnlySpan<string?> FieldTypes { get; }

    /// <summary>Boxed field values — used for binary encoding and generic paths. Allocates.</summary>
    object?[] FieldValues { get; }

    /// <summary>
    /// Write all field values directly to an AsonWriter without boxing.
    /// Default implementation falls back to FieldValues (boxed). Override for max perf.
    /// </summary>
    void WriteValues(ref AsonWriter w)
    {
        var vals = FieldValues;
        for (int i = 0; i < vals.Length; i++)
        {
            if (i > 0) w.WriteChar(',');
            w.WriteValue(vals[i]);
        }
    }

    /// <summary>
    /// Write all field values directly to a BinWriter without boxing.
    /// Default implementation falls back to FieldValues (boxed).
    /// </summary>
    void WriteBinaryValues(ref BinWriter w)
    {
        var vals = FieldValues;
        for (int i = 0; i < vals.Length; i++)
            BinaryCodec.WriteBinaryValue(ref w, vals[i]);
    }
}

/// <summary>ASON type annotation constants.</summary>
public static class AsonType
{
    public const string Int = "int";
    public const string Float = "float";
    public const string Str = "str";
    public const string Bool = "bool";
}
