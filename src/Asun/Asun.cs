namespace Asun;

/// <summary>
/// Public ASUN API. All methods use the naming convention: encode, decode, encodeBinary, etc.
/// </summary>
public static class Asun
{
    // --- Text Encoding ---
    public static string encode(IAsunSchema value) => Encoder.Encode(value);
    public static string encode<T>(IReadOnlyList<T> values) where T : IAsunSchema => Encoder.Encode(values);
    public static string encodeTyped(IAsunSchema value) => Encoder.EncodeTyped(value);
    public static string encodeTyped<T>(IReadOnlyList<T> values) where T : IAsunSchema => Encoder.EncodeTyped(values);

    // --- Text Decoding ---
    public static Dictionary<string, object?> decode(string input) => Decoder.Decode(input);
    public static T decodeWith<T>(string input, Func<Dictionary<string, object?>, T> factory) => Decoder.DecodeWith(input, factory);
    public static List<Dictionary<string, object?>> decodeList(string input) => Decoder.DecodeList(input);
    public static List<T> decodeListWith<T>(string input, Func<Dictionary<string, object?>, T> factory) => Decoder.DecodeListWith(input, factory);

    // --- Untyped Value Codec (for schema-less / dynamic data) ---
    public static string encodeValue(AsunValue value) => AsunValueCodec.Encode(value);
    public static AsunValue decodeValue(string input) => AsunValueCodec.Decode(input);

    // --- Binary Encoding ---
    public static byte[] encodeBinary(IAsunSchema value) => BinaryCodec.EncodeBinary(value);
    public static byte[] encodeBinary<T>(IReadOnlyList<T> values) where T : IAsunSchema => BinaryCodec.EncodeBinary(values);

    // --- Binary Decoding ---
    public static T decodeBinaryWith<T>(byte[] data, string[] fields, FieldType[] types, Func<Dictionary<string, object?>, T> factory) =>
        BinaryCodec.DecodeBinaryWith<T>(data, fields, types, factory);
    public static List<T> decodeBinaryListWith<T>(byte[] data, string[] fields, FieldType[] types, Func<Dictionary<string, object?>, T> factory) =>
        BinaryCodec.DecodeBinaryListWith<T>(data, fields, types, factory);

    // --- Pretty Encoding ---
    public static string encodePretty(IAsunSchema value) => PrettyPrinter.EncodePretty(value);
    public static string encodePretty<T>(IReadOnlyList<T> values) where T : IAsunSchema => PrettyPrinter.EncodePretty(values);
    public static string encodePrettyTyped(IAsunSchema value) => PrettyPrinter.EncodePrettyTyped(value);
    public static string encodePrettyTyped<T>(IReadOnlyList<T> values) where T : IAsunSchema => PrettyPrinter.EncodePrettyTyped(values);
}
