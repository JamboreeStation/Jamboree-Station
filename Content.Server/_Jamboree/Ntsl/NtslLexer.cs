// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// This turns NTSL source code into a list of tokens.
/// </summary>
public sealed class NtslLexer
{
    private static readonly Dictionary<string, NtslTokenType> Keywords = new()
    {
        ["def"] = NtslTokenType.Def,
        ["if"] = NtslTokenType.If,
        ["elseif"] = NtslTokenType.Elseif,
        ["else"] = NtslTokenType.Else,
        ["while"] = NtslTokenType.While,
        ["for"] = NtslTokenType.For,
        ["return"] = NtslTokenType.Return,
        ["break"] = NtslTokenType.Break,
        ["continue"] = NtslTokenType.Continue,
    };

    private readonly string _source;
    private readonly List<NtslToken> _tokens = new();
    private int _pos;
    private int _line = 1;

    public NtslLexer(string source)
    {
        _source = source;
    }

    public List<NtslToken> Tokenize()
    {
        while (_pos < _source.Length)
        {
            var c = _source[_pos];

            if (c == '\n')
            {
                _line++;
                _pos++;
            }
            else if (char.IsWhiteSpace(c))
            {
                _pos++;
            }
            else if (c == '/' && Peek(1) == '/')
            {
                SkipLineComment();
            }
            else if (c == '/' && Peek(1) == '*')
            {
                SkipBlockComment();
            }
            else if (char.IsAsciiDigit(c))
            {
                ReadNumber();
            }
            else if (c == '"' || c == '\'')
            {
                ReadString(c);
            }
            else if (IsNameStart(c))
            {
                ReadName();
            }
            else
            {
                ReadSymbol(c);
            }
        }

        _tokens.Add(new NtslToken(NtslTokenType.End, string.Empty, _line));
        return _tokens;
    }

    private char Peek(int offset)
    {
        var index = _pos + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private static bool IsNameStart(char c)
    {
        return char.IsAsciiLetter(c) || c == '_' || c == '$';
    }

    private static bool IsNamePart(char c)
    {
        return IsNameStart(c) || char.IsAsciiDigit(c);
    }

    private void SkipLineComment()
    {
        while (_pos < _source.Length && _source[_pos] != '\n')
            _pos++;
    }

    private void SkipBlockComment()
    {
        var startLine = _line;
        _pos += 2;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '*' && Peek(1) == '/')
            {
                _pos += 2;
                return;
            }

            if (_source[_pos] == '\n')
                _line++;

            _pos++;
        }

        throw new NtslException("ntsl-error-unclosed-comment", startLine);
    }

    private void ReadNumber()
    {
        var start = _pos;
        var seenDot = false;

        while (_pos < _source.Length)
        {
            var c = _source[_pos];
            if (c == '.' && !seenDot && char.IsAsciiDigit(Peek(1)))
                seenDot = true;
            else if (!char.IsAsciiDigit(c))
                break;

            _pos++;
        }

        Add(NtslTokenType.Number, _source[start.._pos]);
    }

    private void ReadString(char quote)
    {
        var text = new StringBuilder();
        _pos++;

        while (true)
        {
            if (_pos >= _source.Length || _source[_pos] == '\n')
                throw new NtslException("ntsl-error-unclosed-string", _line);

            var c = _source[_pos++];
            if (c == quote)
                break;

            if (c == '\\' && _pos < _source.Length)
            {
                var escaped = _source[_pos++];
                text.Append(escaped == 'n' ? '\n' : escaped);
                continue;
            }

            text.Append(c);
        }

        Add(NtslTokenType.String, text.ToString());
    }

    private void ReadName()
    {
        var start = _pos;
        while (_pos < _source.Length && IsNamePart(_source[_pos]))
            _pos++;

        var name = _source[start.._pos];
        var type = Keywords.TryGetValue(name, out var keyword) ? keyword : NtslTokenType.Identifier;
        Add(type, name);
    }

    private void ReadSymbol(char c)
    {
        var (type, length) = (c, Peek(1)) switch
        {
            ('=', '=') => (NtslTokenType.Equal, 2),
            ('!', '=') => (NtslTokenType.NotEqual, 2),
            ('<', '=') => (NtslTokenType.LessEqual, 2),
            ('>', '=') => (NtslTokenType.GreaterEqual, 2),
            ('&', '&') => (NtslTokenType.And, 2),
            ('|', '|') => (NtslTokenType.Or, 2),
            ('+', '=') => (NtslTokenType.PlusAssign, 2),
            ('-', '=') => (NtslTokenType.MinusAssign, 2),
            ('=', _) => (NtslTokenType.Assign, 1),
            ('!', _) => (NtslTokenType.Not, 1),
            ('<', _) => (NtslTokenType.Less, 1),
            ('>', _) => (NtslTokenType.Greater, 1),
            ('+', _) => (NtslTokenType.Plus, 1),
            ('-', _) => (NtslTokenType.Minus, 1),
            ('*', _) => (NtslTokenType.Star, 1),
            ('/', _) => (NtslTokenType.Slash, 1),
            ('%', _) => (NtslTokenType.Percent, 1),
            ('^', _) => (NtslTokenType.Caret, 1),
            ('(', _) => (NtslTokenType.LeftParen, 1),
            (')', _) => (NtslTokenType.RightParen, 1),
            ('{', _) => (NtslTokenType.LeftBrace, 1),
            ('}', _) => (NtslTokenType.RightBrace, 1),
            ('[', _) => (NtslTokenType.LeftBracket, 1),
            (']', _) => (NtslTokenType.RightBracket, 1),
            ('.', _) => (NtslTokenType.Dot, 1),
            (',', _) => (NtslTokenType.Comma, 1),
            (';', _) => (NtslTokenType.Semicolon, 1),
            _ => throw new NtslException("ntsl-error-unexpected-character", _line, ("character", c.ToString())),
        };

        Add(type, _source.Substring(_pos, length));
        _pos += length;
    }

    private void Add(NtslTokenType type, string text)
    {
        _tokens.Add(new NtslToken(type, text, _line));
    }
}