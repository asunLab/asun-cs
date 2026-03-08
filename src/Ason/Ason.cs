namespace Ason;

/// <summary>
/// Public ASON API. All methods use the naming convention: encode, decode, encodeBinary, etc.
/// </summary>
public static class Ason
{
    // --- Text Encoding ---
    public static string encode(IAsonSchema value) => Encoder.Encode(value);
    public static string encode<T>(IReadOnlyList<T> values) where T : IAsonSchema => Encoder.Encode(values);
    public static string encodeTyped(IAsonSchema value) => Encoder.EncodeTyped(value);
    public static string encodeTyped<T>(IReadOnlyList<T> values) where T : IAsonSchema => Encoder.EncodeTyped(values);

    // --- Text Decoding ---
    public static Dictionary<string, object?> decode(string input) => Decoder.Decode(input);
    public static T decodeWith<T>(string input, Func<Dictionary<string, object?>, T> factory) => Decoder.DecodeWith(input, factory);
    public static List<Dictionary<string, object?>> decodeList(string input) => Decoder.DecodeList(input);
    public static List<T> decodeListWith<T>(string input, Func<Dictionary<string, object?>, T> factory) => Decoder.DecodeListWith(input, factory);

    // --- Binary Encoding ---
    public static byte[] encodeBinary(IAsonSchema value) => BinaryCodec.EncodeBinary(value);
    public static byte[] encodeBinary<T>(IReadOnlyList<T> values) where T : IAsonSchema => BinaryCodec.EncodeBinary(values);

    // --- Binary Decoding ---
    public static T decodeBinaryWith<T>(byte[] data, string[] fields, FieldType[] types, Func<Dictionary<string, object?>, T> factory) =>
        BinaryCodec.DecodeBinaryWith<T>(data, fields, types, factory);
    public static List<T> decodeBinaryListWith<T>(byte[] data, string[] fields, FieldType[] types, Func<Dictionary<string, object?>, T> factory) =>
        BinaryCodec.DecodeBinaryListWith<T>(data, fields, types, factory);

    // --- Pretty Encoding ---
    public static string encodePretty(IAsonSchema value) => PrettyPrinter.EncodePretty(value);
    public static string encodePretty<T>(IReadOnlyList<T> values) where T : IAsonSchema => PrettyPrinter.EncodePretty(values);
    public static string encodePrettyTyped(IAsonSchema value) => PrettyPrinter.EncodePrettyTyped(value);
    public static string encodePrettyTyped<T>(IReadOnlyList<T> values) where T : IAsonSchema => PrettyPrinter.EncodePrettyTyped(values);
}
