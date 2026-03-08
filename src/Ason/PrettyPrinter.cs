namespace Ason;

/// <summary>Pretty-format ASON output with smart indentation.</summary>
public static class PrettyPrinter
{
    private const int MaxWidth = 100;

    public static string EncodePretty(IAsonSchema value) => PrettyFormat(Encoder.Encode(value));
    public static string EncodePretty<T>(IReadOnlyList<T> values) where T : IAsonSchema => PrettyFormat(Encoder.Encode(values));
    public static string EncodePrettyTyped(IAsonSchema value) => PrettyFormat(Encoder.EncodeTyped(value));
    public static string EncodePrettyTyped<T>(IReadOnlyList<T> values) where T : IAsonSchema => PrettyFormat(Encoder.EncodeTyped(values));

    public static string PrettyFormat(string src)
    {
        if (src.Length == 0) return "";
        var mat = BuildMatchTable(src);
        var f = new Formatter(src, mat);
        f.WriteTop();
        return f.Output.ToString();
    }

    private static int[] BuildMatchTable(string src)
    {
        int n = src.Length;
        var mat = new int[n];
        Array.Fill(mat, -1);
        var stack = new Stack<int>();
        bool inQuote = false;
        int i = 0;
        while (i < n)
        {
            char c = src[i];
            if (inQuote) { if (c == '\\' && i + 1 < n) { i += 2; continue; } if (c == '"') inQuote = false; i++; continue; }
            switch (c)
            {
                case '"': inQuote = true; break;
                case '{': case '(': case '[': stack.Push(i); break;
                case '}': case ')': case ']':
                    if (stack.Count > 0) { int j = stack.Pop(); mat[j] = i; mat[i] = j; }
                    break;
            }
            i++;
        }
        return mat;
    }

    private ref struct Formatter
    {
        private readonly string _src;
        private readonly int[] _mat;
        internal readonly System.Text.StringBuilder Output;
        private int _pos;
        private int _depth;

        public Formatter(string src, int[] mat) { _src = src; _mat = mat; Output = new(); _pos = 0; _depth = 0; }

        internal void WriteTop()
        {
            if (_pos >= _src.Length) return;
            char c = _src[_pos];
            if (c == '[' && _pos + 1 < _src.Length && _src[_pos + 1] == '{') WriteArrayTop();
            else if (c == '{') WriteObjectTop();
            else Output.Append(_src.AsSpan(_pos));
        }

        private void WriteObjectTop()
        {
            WriteGroup();
            if (_pos < _src.Length && _src[_pos] == ':')
            {
                Output.Append(':');
                _pos++;
                if (_pos < _src.Length)
                {
                    int close = _mat[_pos];
                    if (close >= 0 && close - _pos + 1 <= MaxWidth) { WriteInline(_pos, close + 1); _pos = close + 1; }
                    else { Output.Append('\n'); _depth++; WriteIndent(); WriteGroup(); _depth--; }
                }
            }
        }

        private void WriteArrayTop()
        {
            Output.Append('['); _pos++;
            WriteGroup();
            if (_pos < _src.Length && _src[_pos] == ']') { Output.Append(']'); _pos++; }
            if (_pos < _src.Length && _src[_pos] == ':') { Output.Append(":\n"); _pos++; }
            _depth++;
            bool first = true;
            while (_pos < _src.Length)
            {
                if (_src[_pos] == ',') _pos++;
                if (_pos >= _src.Length) break;
                if (!first) Output.Append(",\n");
                first = false;
                WriteIndent();
                WriteGroup();
            }
            Output.Append('\n');
            _depth--;
        }

        private void WriteGroup()
        {
            if (_pos >= _src.Length) return;
            char ch = _src[_pos];
            if (ch != '{' && ch != '(' && ch != '[') { WriteValue(); return; }

            if (ch == '[' && _pos + 1 < _src.Length && _src[_pos + 1] == '{')
            {
                int closeBrace = _mat[_pos + 1];
                int closeBracket = _mat[_pos];
                if (closeBrace >= 0 && closeBracket >= 0 && closeBrace + 1 == closeBracket)
                {
                    int width = closeBracket - _pos + 1;
                    if (width <= MaxWidth) { WriteInline(_pos, closeBracket + 1); _pos = closeBracket + 1; return; }
                    Output.Append('['); _pos++; WriteGroup(); Output.Append(']'); _pos++; return;
                }
            }

            int closePos = _mat[_pos];
            if (closePos < 0) { Output.Append(ch); _pos++; return; }
            int w = closePos - _pos + 1;
            if (w <= MaxWidth) { WriteInline(_pos, closePos + 1); _pos = closePos + 1; return; }

            char closeCh = _src[closePos];
            Output.Append(ch); _pos++;
            if (_pos >= closePos) { Output.Append(closeCh); _pos = closePos + 1; return; }
            Output.Append('\n'); _depth++;
            bool first = true;
            while (_pos < closePos)
            {
                if (_src[_pos] == ',') _pos++;
                if (!first) Output.Append(",\n");
                first = false;
                WriteIndent();
                WriteElement(closePos);
            }
            Output.Append('\n'); _depth--;
            WriteIndent();
            Output.Append(closeCh); _pos = closePos + 1;
        }

        private void WriteElement(int boundary)
        {
            while (_pos < boundary && _src[_pos] != ',')
            {
                char ch = _src[_pos];
                if (ch == '{' || ch == '(' || ch == '[') WriteGroup();
                else if (ch == '"') WriteQuoted();
                else { Output.Append(ch); _pos++; }
            }
        }

        private void WriteValue()
        {
            while (_pos < _src.Length)
            {
                char ch = _src[_pos];
                if (ch == ',' || ch == ')' || ch == '}' || ch == ']') break;
                if (ch == '"') WriteQuoted();
                else { Output.Append(ch); _pos++; }
            }
        }

        private void WriteQuoted()
        {
            Output.Append('"'); _pos++;
            while (_pos < _src.Length)
            {
                char ch = _src[_pos];
                Output.Append(ch); _pos++;
                if (ch == '\\' && _pos < _src.Length) { Output.Append(_src[_pos]); _pos++; }
                else if (ch == '"') break;
            }
        }

        private void WriteInline(int start, int end)
        {
            int depth = 0;
            bool inQuote = false;
            int i = start;
            while (i < end)
            {
                char ch = _src[i];
                if (inQuote)
                {
                    Output.Append(ch);
                    if (ch == '\\' && i + 1 < end) { i++; Output.Append(_src[i]); }
                    else if (ch == '"') inQuote = false;
                    i++; continue;
                }
                switch (ch)
                {
                    case '"': inQuote = true; Output.Append(ch); break;
                    case '{': case '(': case '[': depth++; Output.Append(ch); break;
                    case '}': case ')': case ']': depth--; Output.Append(ch); break;
                    case ',': Output.Append(','); if (depth == 1) Output.Append(' '); break;
                    default: Output.Append(ch); break;
                }
                i++;
            }
        }

        private void WriteIndent()
        {
            for (int i = 0; i < _depth; i++) Output.Append("  ");
        }
    }
}
