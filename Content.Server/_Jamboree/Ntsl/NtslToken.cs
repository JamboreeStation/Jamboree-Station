// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Jamboree.Ntsl;

public enum NtslTokenType : byte
{
    Number,
    String,
    Identifier,

    Def,
    If,
    Elseif,
    Else,
    While,
    For,
    Return,
    Break,
    Continue,

    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Dot,
    Comma,
    Semicolon,

    Assign,
    PlusAssign,
    MinusAssign,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Caret,

    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    And,
    Or,
    Not,

    End,
}

public readonly record struct NtslToken(NtslTokenType Type, string Text, int Line);

/// <summary>
/// A mistake in a player's script... holds a locale id so the console can show it.
/// </summary>
public sealed class NtslException(string locId, int line, params (string, object)[] args) : Exception(locId)
{
    public int Line { get; } = line;
    public (string, object)[] Args { get; } = args;
}