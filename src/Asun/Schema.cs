// ASUN — Array-Schema Unified Notation for .NET
using System.Text.Json.Serialization;

namespace Asun;

/// <summary>Schema descriptor for ASUN struct serialization.</summary>
public interface IAsunSchema
{
    ReadOnlySpan<string> FieldNames { get; }
    ReadOnlySpan<string?> FieldTypes { get; }

    /// <summary>Boxed field values — used for binary encoding and generic paths. Allocates.</summary>
    object?[] FieldValues { get; }

    /// <summary>
    /// Write all field values directly to an AsunWriter without boxing.
    /// Default implementation falls back to FieldValues (boxed). Override for max perf.
    /// </summary>
    void WriteValues(ref AsunWriter w)
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

/// <summary>ASUN type annotation constants.</summary>
public static class AsunType
{
    public const string Int = "int";
    public const string Float = "float";
    public const string Str = "str";
    public const string Bool = "bool";
}
