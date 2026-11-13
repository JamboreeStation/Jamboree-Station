// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Client._Jamboree.Telecomms;

/// <summary>
/// Tab finishes a half-typed name.
/// </summary>
public sealed class NtslEditorHints
{
    private static readonly Dictionary<string, string> Functions = new()
    {
        ["length"] = "value",
        ["lower"] = "text",
        ["upper"] = "text",
        ["proper"] = "text",
        ["reverse"] = "value",
        ["repeat"] = "text, times",
        ["trim"] = "text",
        ["find"] = "text, part",
        ["substr"] = "text, start, end",
        ["replace"] = "text, find, with",
        ["explode"] = "text, separator",
        ["implode"] = "list, separator",
        ["tostring"] = "value",
        ["tonum"] = "text",
        ["abs"] = "number",
        ["round"] = "number",
        ["floor"] = "number",
        ["ceil"] = "number",
        ["sqrt"] = "number",
        ["log"] = "number, base",
        ["sin"] = "degrees",
        ["cos"] = "degrees",
        ["tan"] = "degrees",
        ["asin"] = "number",
        ["acos"] = "number",
        ["atan"] = "number",
        ["min"] = "a, b, ...",
        ["max"] = "a, b, ...",
        ["clamp"] = "number, low, high",
        ["inrange"] = "number, low, high",
        ["prob"] = "percent",
        ["rand"] = "low, high",
        ["pick"] = "a, b, ...",
        ["signal"] = "content, freq, source, job",
        ["broadcast"] = "sig or content, freq, source",
        ["mem"] = "key, value",
        ["clearmem"] = "",
        ["time"] = "",
        ["accent"] = "text",
        ["list"] = "item, ...",
    };

    private static readonly Dictionary<string, string> ListMethods = new()
    {
        ["Find"] = "value",
        ["Join"] = "separator",
        ["Copy"] = "start, end",
        ["Cut"] = "start, end",
        ["Add"] = "item, ...",
        ["Remove"] = "item, ...",
        ["Insert"] = "index, item, ...",
        ["Swap"] = "a, b",
    };

    private static readonly string[] Words =
    {
        "def", "if", "elseif", "else", "while", "for", "return", "break", "continue",
        "true", "false", "null", "PI", "E", "SQRT2", "NORTH", "SOUTH", "EAST", "WEST",
        "process_signal", "channels", "filter_types",
    };

    private static readonly string[] Members = { "content", "raw", "source", "job", "freq", "channel", "language", "filters", "pass", "len" };
    private static readonly string[] FilterTypes = { "robot", "loud", "emphasis", "wacky", "commanding" };

    private static readonly Regex DefRegex = new(@"\bdef\s+([A-Za-z_$][\w$]*)\s*\(([^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex AssignRegex = new(@"(?<![\w$.""'])([A-Za-z_$][\w$]*)\s*[+-]?=(?!=)", RegexOptions.Compiled);

    private readonly string[] _channels;
    private readonly Dictionary<string, string> _userFunctions = new();
    private readonly HashSet<string> _userNames = new();
    private string _code = string.Empty;

    private readonly record struct Call(string Name, bool Member, int Argument);

    public NtslEditorHints(IEnumerable<string> channels)
    {
        _channels = channels.ToArray();
    }

    /// <summary>
    /// Picks up the functions and variables the script defines itself.
    /// </summary>
    public void SetCode(string code)
    {
        _code = code;
        _userFunctions.Clear();
        _userNames.Clear();

        foreach (Match match in DefRegex.Matches(code))
        {
            var parameters = match.Groups[2].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            _userFunctions[match.Groups[1].Value] = string.Join(", ", parameters);
            _userNames.UnionWith(parameters);
        }

        foreach (Match match in AssignRegex.Matches(code))
        {
            _userNames.Add(match.Groups[1].Value);
        }
    }

    /// <summary>
    /// What Tab should type at the cursor, or null when Tab should indent instead.
    /// </summary>
    public string? Complete(int cursor)
    {
        cursor = Math.Clamp(cursor, 0, _code.Length);
        var choices = Choices(cursor, out var word, out var isFunction);
        if (choices.Count == 0)
            return null;

        if (choices.Count > 1)
            return CommonPrefix(choices)[word.Length..];

        var rest = choices[0][word.Length..];
        if (isFunction(choices[0]) && (cursor >= _code.Length || _code[cursor] != '('))
            rest += "(";

        return rest.Length > 0 ? rest : null;
    }

    /// <summary>
    /// The names Tab can finish the word at the cursor with.
    /// </summary>
    public List<string> Choices(int cursor)
    {
        cursor = Math.Clamp(cursor, 0, _code.Length);
        var choices = Choices(cursor, out var word, out _);
        return choices.Count == 1 && choices[0] == word ? new List<string>() : choices;
    }

    /// <summary>
    /// The innermost known function call around the cursor, and which of its arguments is being typed.
    /// </summary>
    public bool TryGetCall(int cursor, out string name, out string[] parameters, out int argument)
    {
        cursor = Math.Clamp(cursor, 0, _code.Length);
        Scan(cursor, out var calls);

        for (var i = calls.Count - 1; i >= 0; i--)
        {
            var call = calls[i];
            var signature = call.Member
                ? ListMethod(call.Name)
                : _userFunctions.GetValueOrDefault(call.Name) ?? Functions.GetValueOrDefault(call.Name);

            if (signature == null)
                continue;

            name = call.Name;
            parameters = signature.Length == 0 ? Array.Empty<string>() : signature.Split(", ");
            argument = call.Argument;
            return true;
        }

        name = string.Empty;
        parameters = Array.Empty<string>();
        argument = 0;
        return false;
    }

    private List<string> Choices(int cursor, out string word, out Func<string, bool> isFunction)
    {
        word = string.Empty;
        isFunction = _ => false;

        if ((cursor < _code.Length && IsNamePart(_code[cursor])) || !Scan(cursor, out _))
            return new List<string>();

        word = WordBefore(cursor, out var start);
        var afterDot = start > 0 && _code[start - 1] == '.';
        if ((word.Length == 0 && !afterDot) || (word.Length > 0 && char.IsAsciiDigit(word[0])))
            return new List<string>();

        IEnumerable<string> names;
        if (!afterDot)
        {
            names = Functions.Keys.Concat(_userFunctions.Keys).Concat(Words).Concat(_userNames);
            isFunction = name => Functions.ContainsKey(name) || _userFunctions.ContainsKey(name);
        }
        else
        {
            switch (WordBefore(start - 1, out _))
            {
                case "channels":
                    names = _channels;
                    break;
                case "filter_types":
                    names = FilterTypes;
                    break;
                default:
                    names = Members.Concat(ListMethods.Keys);
                    isFunction = ListMethods.ContainsKey;
                    break;
            }
        }

        var typed = word;
        var choices = names
            .Where(name => name.StartsWith(typed, StringComparison.Ordinal))
            .Distinct()
            .ToList();

        choices.Sort((a, b) => a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b));
        return choices;
    }

    private static string? ListMethod(string name)
    {
        foreach (var (method, signature) in ListMethods)
        {
            if (method.Equals(name, StringComparison.OrdinalIgnoreCase))
                return signature;
        }

        return null;
    }

    /// <summary>
    /// Reads the code up to the cursor the same way the lexer does and keeps track of the open calls.
    /// Returns false when the cursor is inside a string or a comment.
    /// </summary>
    private bool Scan(int cursor, out List<Call> calls)
    {
        calls = new List<Call>();
        var i = 0;
        while (i < cursor)
        {
            var c = _code[i];
            var next = i + 1 < _code.Length ? _code[i + 1] : '\0';

            if (c is '"' or '\'')
            {
                var end = i + 1;
                while (end < _code.Length && _code[end] != c && _code[end] != '\n')
                {
                    end += _code[end] == '\\' ? 2 : 1;
                }

                if (end >= cursor)
                    return false;

                i = end + 1;
                continue;
            }

            if (c == '/' && next == '/')
            {
                var end = _code.IndexOf('\n', i);
                if (end < 0 || end >= cursor)
                {
                    calls.Clear();
                    return false;
                }

                i = end;
                continue;
            }

            if (c == '/' && next == '*')
            {
                var end = _code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end < 0 || end + 2 > cursor)
                {
                    calls.Clear();
                    return false;
                }

                i = end + 2;
                continue;
            }

            switch (c)
            {
                case '(':
                    calls.Add(CallAt(i));
                    break;
                case ')' when calls.Count > 0:
                    calls.RemoveAt(calls.Count - 1);
                    break;
                case ',' when calls.Count > 0:
                    calls[^1] = calls[^1] with { Argument = calls[^1].Argument + 1 };
                    break;
                case ';' or '{' or '}':
                    calls.Clear();
                    break;
            }

            i++;
        }

        return true;
    }

    private Call CallAt(int paren)
    {
        var end = paren;
        while (end > 0 && _code[end - 1] is ' ' or '\t')
        {
            end--;
        }

        var name = WordBefore(end, out var start);
        return new Call(name, start > 0 && _code[start - 1] == '.', 0);
    }

    private string WordBefore(int index, out int start)
    {
        start = index;
        while (start > 0 && IsNamePart(_code[start - 1]))
        {
            start--;
        }

        return _code[start..index];
    }

    private static bool IsNamePart(char c)
    {
        return char.IsAsciiLetterOrDigit(c) || c == '_' || c == '$';
    }

    private static string CommonPrefix(List<string> names)
    {
        var prefix = names[0];
        foreach (var name in names)
        {
            var length = 0;
            while (length < prefix.Length && length < name.Length && prefix[length] == name[length])
            {
                length++;
            }

            prefix = prefix[..length];
        }

        return prefix;
    }
}
