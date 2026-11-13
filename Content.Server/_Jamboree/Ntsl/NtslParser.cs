// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// Turns NTSL tokens into a tree of statements!
/// </summary>
public sealed class NtslParser
{
    public const int MaxDepth = 100;

    private readonly List<NtslToken> _tokens;
    private int _pos;
    private int _depth;

    public NtslParser(List<NtslToken> tokens)
    {
        _tokens = tokens;
    }

    public List<NtslStmt> ParseProgram()
    {
        var statements = new List<NtslStmt>();
        while (Current.Type != NtslTokenType.End)
        {
            if (TryParseStatement() is { } statement)
                statements.Add(statement);
        }

        return statements;
    }

    private NtslToken Current => _tokens[_pos];

    private NtslToken Peek(int offset)
    {
        return _tokens[Math.Min(_pos + offset, _tokens.Count - 1)];
    }

    private NtslToken Advance()
    {
        var token = _tokens[_pos];
        if (token.Type != NtslTokenType.End)
            _pos++;

        return token;
    }

    private bool Match(NtslTokenType type)
    {
        if (Current.Type != type)
            return false;

        _pos++;
        return true;
    }

    private NtslToken Expect(NtslTokenType type, string symbol)
    {
        if (Current.Type != type)
            throw Unexpected("ntsl-error-expected-symbol", ("symbol", symbol));

            return Advance();
    }

    private NtslToken ExpectName()
    {
        if (Current.Type != NtslTokenType.Identifier)
            throw Unexpected("ntsl-error-expected-name");

        return Advance();
    }

    private NtslException Unexpected(string locId, params (string, object)[] args)
    {
        if (Current.Type == NtslTokenType.End)
            return new NtslException("ntsl-error-unexpected-end", Current.Line);

        return new NtslException(locId, Current.Line, [..args, ("found", Current.Text)]);
    }

    private void Enter()
    {
        if (++_depth > MaxDepth)
            throw new NtslException("ntsl-error-too-deep", Current.Line);
    }

    private NtslStmt? TryParseStatement()
    {
        var token = Current;
        switch (token.Type)
        {
            case NtslTokenType.Semicolon:
                Advance();
                return null;
            case NtslTokenType.Def:
                return ParseDef();
            case NtslTokenType.If:
                return ParseIf();
            case NtslTokenType.While:
                return ParseWhile();
            case NtslTokenType.For:
                return ParseFor();
            case NtslTokenType.Return:
            {
                Advance();
                var value = Current.Type == NtslTokenType.Semicolon ? null : ParseExpression();
                Expect(NtslTokenType.Semicolon, ";");
                return new NtslReturnStmt(token.Line, value);
            }
            case NtslTokenType.Break:
                Advance();
                Expect(NtslTokenType.Semicolon, ";");
                return new NtslBreakStmt(token.Line);
            case NtslTokenType.Continue:
                Advance();
                Expect(NtslTokenType.Semicolon, ";");
                return new NtslContinueStmt(token.Line);
            default:
            {
                var expr = ParseExpression();
                Expect(NtslTokenType.Semicolon, ";");
                return new NtslExprStmt(token.Line, expr);
            }
        }
    }

    private List<NtslStmt> ParseBlock()
    {
        Expect(NtslTokenType.LeftBrace, "{");
        Enter();

        var body = new List<NtslStmt>();
        while (!Match(NtslTokenType.RightBrace))
        {
            if (Current.Type == NtslTokenType.End)
                throw Unexpected("ntsl-error-expected-symbol", ("symbol", "}"));

                if (TryParseStatement() is { } statement)
                    body.Add(statement);
        }

        _depth--;
        return body;
    }

    private NtslExpr ParseCondition()
    {
        Expect(NtslTokenType.LeftParen, "(");
        var condition = ParseExpression();
        Expect(NtslTokenType.RightParen, ")");
        return condition;
    }

    private NtslStmt ParseDef()
    {
        var line = Advance().Line;
        var name = ExpectName().Text;
        Expect(NtslTokenType.LeftParen, "(");

        var parameters = new List<string>();
        if (!Match(NtslTokenType.RightParen))
        {
            do
            {
                parameters.Add(ExpectName().Text);
            } while (Match(NtslTokenType.Comma));

            Expect(NtslTokenType.RightParen, ")");
        }

        return new NtslDefStmt(line, name, parameters, ParseBlock());
    }

    private NtslStmt ParseIf()
    {
        var line = Advance().Line;
        var branches = new List<NtslIfBranch> { new(ParseCondition(), ParseBlock()) };
        List<NtslStmt>? elseBody = null;

        while (true)
        {
            if (Match(NtslTokenType.Elseif))
            {
                branches.Add(new NtslIfBranch(ParseCondition(), ParseBlock()));
            }
            else if (Match(NtslTokenType.Else))
            {
                if (Match(NtslTokenType.If))
                {
                    branches.Add(new NtslIfBranch(ParseCondition(), ParseBlock()));
                    continue;
                }

                elseBody = ParseBlock();
                break;
            }
            else
            {
                break;
            }
        }

        return new NtslIfStmt(line, branches, elseBody);
    }

    private NtslStmt ParseWhile()
    {
        var line = Advance().Line;
        return new NtslWhileStmt(line, ParseCondition(), ParseBlock());
    }

    private NtslStmt ParseFor()
    {
        var line = Advance().Line;
        Expect(NtslTokenType.LeftParen, "(");
        var init = Current.Type == NtslTokenType.Semicolon ? null : ParseExpression();
        Expect(NtslTokenType.Semicolon, ";");
        var condition = Current.Type == NtslTokenType.Semicolon ? null : ParseExpression();
        Expect(NtslTokenType.Semicolon, ";");
        var step = Current.Type == NtslTokenType.RightParen ? null : ParseExpression();
        Expect(NtslTokenType.RightParen, ")");
        return new NtslForStmt(line, init, condition, step, ParseBlock());
    }

    private NtslExpr ParseExpression()
    {
        return ParseAssignment();
    }

    private NtslExpr ParseAssignment()
    {
        var target = ParseOr();
        if (Current.Type is not (NtslTokenType.Assign or NtslTokenType.PlusAssign or NtslTokenType.MinusAssign))
            return target;

        var op = Advance();
        if (target is not (NtslNameExpr or NtslMemberExpr or NtslIndexExpr))
            throw new NtslException("ntsl-error-invalid-assignment", op.Line);

        return new NtslAssignExpr(op.Line, op.Type, target, ParseExpression());
    }

    private NtslExpr ParseBinary(Func<NtslExpr> next, params NtslTokenType[] operators)
    {
        var left = next();
        while (Array.IndexOf(operators, Current.Type) >= 0)
        {
            var op = Advance();
            left = new NtslBinaryExpr(op.Line, op.Type, left, next());
        }

        return left;
    }

    private NtslExpr ParseOr()
    {
        return ParseBinary(ParseAnd, NtslTokenType.Or);
    }

    private NtslExpr ParseAnd()
    {
        return ParseBinary(ParseEquality, NtslTokenType.And);
    }

    private NtslExpr ParseEquality()
    {
        return ParseBinary(ParseComparison, NtslTokenType.Equal, NtslTokenType.NotEqual);
    }

    private NtslExpr ParseComparison()
    {
        return ParseBinary(ParseAdditive, NtslTokenType.Less, NtslTokenType.LessEqual, NtslTokenType.Greater, NtslTokenType.GreaterEqual);
    }

    private NtslExpr ParseAdditive()
    {
        return ParseBinary(ParseMultiplicative, NtslTokenType.Plus, NtslTokenType.Minus);
    }

    private NtslExpr ParseMultiplicative()
    {
        return ParseBinary(ParseUnary, NtslTokenType.Star, NtslTokenType.Slash, NtslTokenType.Percent);
    }

    private NtslExpr ParseUnary()
    {
        Enter();

        NtslExpr result;
        if (Current.Type is NtslTokenType.Not or NtslTokenType.Minus)
        {
            var op = Advance();
            result = new NtslUnaryExpr(op.Line, op.Type, ParseUnary());
        }
        else
        {
            result = ParsePower();
        }

        _depth--;
        return result;
    }

    private NtslExpr ParsePower()
    {
        var left = ParsePostfix();
        if (Current.Type != NtslTokenType.Caret)
            return left;

        var op = Advance();
        return new NtslBinaryExpr(op.Line, op.Type, left, ParseUnary());
    }

    private NtslExpr ParsePostfix()
    {
        var expr = ParsePrimary();
        while (true)
        {
            var token = Current;
            if (Match(NtslTokenType.LeftParen))
            {
                expr = new NtslCallExpr(token.Line, expr, ParseArguments());
            }
            else if (Match(NtslTokenType.Dot))
            {
                expr = new NtslMemberExpr(token.Line, expr, ExpectName().Text);
            }
            else if (Match(NtslTokenType.LeftBracket))
            {
                var index = ParseExpression();
                Expect(NtslTokenType.RightBracket, "]");
                expr = new NtslIndexExpr(token.Line, expr, index);
            }
            else
            {
                return expr;
            }
        }
    }

    private List<NtslExpr> ParseArguments()
    {
        var args = new List<NtslExpr>();
        if (Match(NtslTokenType.RightParen))
            return args;

        do
        {
            args.Add(ParseExpression());
        } while (Match(NtslTokenType.Comma));

        Expect(NtslTokenType.RightParen, ")");
        return args;
    }

    private NtslExpr ParsePrimary()
    {
        var token = Current;
        switch (token.Type)
        {
            case NtslTokenType.Number:
                Advance();
                return new NtslNumberExpr(token.Line, double.Parse(token.Text, CultureInfo.InvariantCulture));
            case NtslTokenType.String:
                Advance();
                return new NtslStringExpr(token.Line, token.Text);
            case NtslTokenType.Identifier when token.Text == "list" && Peek(1).Type == NtslTokenType.LeftParen:
                Advance();
                Advance();
                return new NtslListExpr(token.Line, ParseListEntries());
            case NtslTokenType.Identifier:
                Advance();
                return new NtslNameExpr(token.Line, token.Text);
            case NtslTokenType.LeftParen:
            {
                Advance();
                var inner = ParseExpression();
                Expect(NtslTokenType.RightParen, ")");
                return inner;
            }
            default:
                throw Unexpected("ntsl-error-expected-expression");
        }
    }

    private List<NtslListEntry> ParseListEntries()
    {
        var entries = new List<NtslListEntry>();
        if (Match(NtslTokenType.RightParen))
            return entries;

        do
        {
            var value = ParseOr();
            entries.Add(Match(NtslTokenType.Assign)
                ? new NtslListEntry(value, ParseOr())
                : new NtslListEntry(null, value));
        } while (Match(NtslTokenType.Comma));

        Expect(NtslTokenType.RightParen, ")");
        return entries;
    }
}