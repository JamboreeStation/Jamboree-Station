// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// Runs a parsed NTSL program, with hard limits so a broken or hostile script can't hurt the server.
/// </summary>
public sealed partial class NtslInterpreter
{
    public const int MaxCodeLength = 20000;
    public const int MaxStatements = 900;
    public const int MaxLoopIterations = 100;
    public const int MaxCallDepth = 16;
    public const int MaxStringLength = 1024;
    public const int MaxListLength = 256;
    public static readonly TimeSpan MaxRunTime = TimeSpan.FromMilliseconds(20);

    private readonly List<NtslStmt> _program;
    private readonly NtslScope _builtins = new(null);
    private readonly NtslScope _globals;
    private readonly Stopwatch _stopwatch = new();
    private int _statements;
    private int _callDepth;
    private object? _returnValue;
    private bool _lastCallReturned;

    private enum Flow : byte
    {
        Normal,
        Break,
        Continue,
        Return,
    }

    public NtslInterpreter(List<NtslStmt> program)
    {
        _program = program;
        _globals = new NtslScope(_builtins);
        RegisterCoreBuiltins();
    }

    public static NtslInterpreter Compile(string code)
    {
        if (code.Length > MaxCodeLength)
            throw new NtslException("ntsl-error-code-too-long", 1, ("max", MaxCodeLength));

        var tokens = new NtslLexer(code).Tokenize();
        return new NtslInterpreter(new NtslParser(tokens).ParseProgram());
    }

    public void Register(string name, NtslBuiltin function)
    {
        _builtins.Declare(name, function);
    }

    public void Define(string name, object? value)
    {
        _builtins.Declare(name, value);
    }

    public void Run()
    {
        BeginRun();
        ExecuteBlock(_program, _globals);
    }

    /// <summary>
    /// Whether the last <see cref="Call"/> ended with a return statement, rather than running off the end.
    /// </summary>
    public bool Returned { get; private set; }

    public bool HasFunction(string name)
    {
        return _globals.TryGet(name, out var value) && value is NtslFunction;
    }

    public object? Call(string name, params object?[] args)
    {
        BeginRun();
        if (!_globals.TryGet(name, out var function))
            throw new NtslException("ntsl-error-unknown-name", 1, ("name", name));

        _lastCallReturned = false;
        var result = CallValue(function, args.ToList(), 1);
        Returned = _lastCallReturned;
        return result;
    }

    private void BeginRun()
    {
        _statements = 0;
        _callDepth = 0;
        _returnValue = null;
        _stopwatch.Restart();
    }

    private void Tick(int line)
    {
        if (++_statements > MaxStatements)
            throw new NtslException("ntsl-error-too-many-statements", line, ("max", MaxStatements));

        if (_stopwatch.Elapsed > MaxRunTime)
            throw new NtslException("ntsl-error-too-slow", line);
    }

    private Flow ExecuteBlock(List<NtslStmt> body, NtslScope scope)
    {
        foreach (var stmt in body)
        {
            if (stmt is NtslDefStmt def)
                scope.Declare(def.Name, new NtslFunction(def, scope));
        }

        foreach (var stmt in body)
        {
            var flow = Execute(stmt, scope);
            if (flow != Flow.Normal)
                return flow;
        }

        return Flow.Normal;
    }

    private Flow Execute(NtslStmt stmt, NtslScope scope)
    {
        Tick(stmt.Line);

        switch (stmt)
        {
            case NtslExprStmt exprStmt:
                Evaluate(exprStmt.Expr, scope);
                return Flow.Normal;

            case NtslIfStmt ifStmt:
                foreach (var branch in ifStmt.Branches)
                {
                    if (IsTrue(Evaluate(branch.Condition, scope)))
                        return ExecuteBlock(branch.Body, scope);
                }

                return ifStmt.Else != null ? ExecuteBlock(ifStmt.Else, scope) : Flow.Normal;

            case NtslWhileStmt whileStmt:
                return ExecuteLoop(whileStmt.Line, whileStmt.Condition, null, whileStmt.Body, scope);

            case NtslForStmt forStmt:
                if (forStmt.Init != null)
                    Evaluate(forStmt.Init, scope);

                return ExecuteLoop(forStmt.Line, forStmt.Condition, forStmt.Step, forStmt.Body, scope);

            case NtslReturnStmt returnStmt:
                _returnValue = returnStmt.Value != null ? Evaluate(returnStmt.Value, scope) : null;
                return Flow.Return;

            case NtslBreakStmt:
                return Flow.Break;

            case NtslContinueStmt:
                return Flow.Continue;

            default:
                return Flow.Normal;
        }
    }

    private Flow ExecuteLoop(int line, NtslExpr? condition, NtslExpr? step, List<NtslStmt> body, NtslScope scope)
    {
        var iterations = 0;
        while (condition == null || IsTrue(Evaluate(condition, scope)))
        {
            if (++iterations > MaxLoopIterations)
                throw new NtslException("ntsl-error-too-many-loops", line, ("max", MaxLoopIterations));

            var flow = ExecuteBlock(body, scope);
            if (flow == Flow.Break)
                break;

            if (flow == Flow.Return)
                return Flow.Return;

            if (step != null)
                Evaluate(step, scope);
        }

        return Flow.Normal;
    }

    private object? Evaluate(NtslExpr expr, NtslScope scope)
    {
        object? value = expr switch
        {
            NtslNumberExpr number => number.Value,
            NtslStringExpr text => text.Value,
            NtslNameExpr name => GetVariable(name, scope),
            NtslListExpr list => EvaluateList(list, scope),
            NtslUnaryExpr unary => EvaluateUnary(unary, scope),
            NtslBinaryExpr binary => EvaluateBinary(binary, scope),
            NtslAssignExpr assign => EvaluateAssign(assign, scope),
            NtslCallExpr call => EvaluateCall(call, scope),
            NtslMemberExpr member => GetMember(Evaluate(member.Target, scope), member.Name, member.Line),
            NtslIndexExpr index => GetIndex(Evaluate(index.Target, scope), Evaluate(index.Index, scope), index.Line),
            _ => null,
        };

        return value is string { Length: > MaxStringLength } longText ? longText[..MaxStringLength] : value;
    }

    private static object? GetVariable(NtslNameExpr name, NtslScope scope)
    {
        if (!scope.TryGet(name.Name, out var value))
            throw new NtslException("ntsl-error-unknown-name", name.Line, ("name", name.Name));

        return value;
    }

    private NtslList EvaluateList(NtslListExpr listExpr, NtslScope scope)
    {
        var list = new NtslList();
        foreach (var entry in listExpr.Entries)
        {
            if (entry.Key == null)
            {
                AddItem(list, Evaluate(entry.Value, scope), listExpr.Line);
                continue;
            }

            var key = ToKey(Evaluate(entry.Key, scope), listExpr.Line);
            SetKey(list, key, Evaluate(entry.Value, scope), listExpr.Line);
        }

        return list;
    }

    private object? EvaluateUnary(NtslUnaryExpr unary, NtslScope scope)
    {
        var value = Evaluate(unary.Operand, scope);
        if (unary.Op == NtslTokenType.Not)
            return IsTrue(value) ? 0.0 : 1.0;

        return -ToNumber(value, unary.Line);
    }

    private object? EvaluateBinary(NtslBinaryExpr binary, NtslScope scope)
    {
        switch (binary.Op)
        {
            case NtslTokenType.And:
                return IsTrue(Evaluate(binary.Left, scope)) && IsTrue(Evaluate(binary.Right, scope)) ? 1.0 : 0.0;
            case NtslTokenType.Or:
                return IsTrue(Evaluate(binary.Left, scope)) || IsTrue(Evaluate(binary.Right, scope)) ? 1.0 : 0.0;
        }

        var left = Evaluate(binary.Left, scope);
        var right = Evaluate(binary.Right, scope);
        return Operate(binary.Op, left, right, binary.Line);
    }

    private object? EvaluateAssign(NtslAssignExpr assign, NtslScope scope)
    {
        var value = Evaluate(assign.Value, scope);
        if (assign.Op != NtslTokenType.Assign)
        {
            var op = assign.Op == NtslTokenType.PlusAssign ? NtslTokenType.Plus : NtslTokenType.Minus;
            value = Operate(op, Evaluate(assign.Target, scope), value, assign.Line);
            if (value is string { Length: > MaxStringLength } longText)
                value = longText[..MaxStringLength];
        }

        switch (assign.Target)
        {
            case NtslNameExpr name:
                scope.Assign(name.Name, value);
                break;
            case NtslMemberExpr member:
                SetMember(Evaluate(member.Target, scope), member.Name, value, member.Line);
                break;
            case NtslIndexExpr index:
                SetIndex(Evaluate(index.Target, scope), Evaluate(index.Index, scope), value, index.Line);
                break;
        }

        return value;
    }

    private object? EvaluateCall(NtslCallExpr call, NtslScope scope)
    {
        object? callee;
        if (call.Callee is NtslMemberExpr member)
        {
            var target = Evaluate(member.Target, scope);
            if (target is NtslList list && !list.Values.ContainsKey(member.Name) && IsListMethod(member.Name))
                return CallListMethod(list, member.Name, EvaluateArgs(call.Args, scope), call.Line);

            callee = GetMember(target, member.Name, member.Line);
        }
        else
        {
            callee = Evaluate(call.Callee, scope);
        }

        return CallValue(callee, EvaluateArgs(call.Args, scope), call.Line);
    }

    private List<object?> EvaluateArgs(List<NtslExpr> exprs, NtslScope scope)
    {
        var args = new List<object?>(exprs.Count);
        foreach (var expr in exprs)
        {
            args.Add(Evaluate(expr, scope));
        }

        return args;
    }

    private object? CallValue(object? callee, List<object?> args, int line)
    {
        return callee switch
        {
            NtslBuiltin builtin => builtin(args, line),
            NtslFunction function => CallFunction(function, args, line),
            _ => throw new NtslException("ntsl-error-not-a-function", line),
        };
    }

    private object? CallFunction(NtslFunction function, List<object?> args, int line)
    {
        if (++_callDepth > MaxCallDepth)
            throw new NtslException("ntsl-error-too-much-recursion", line, ("max", MaxCallDepth));

        var scope = new NtslScope(function.Closure);
        var parameters = function.Def.Params;
        for (var i = 0; i < parameters.Count; i++)
        {
            scope.Declare(parameters[i], i < args.Count ? args[i] : null);
        }

        var flow = ExecuteBlock(function.Def.Body, scope);
        var result = flow == Flow.Return ? _returnValue : null;
        _lastCallReturned = flow == Flow.Return;
        _returnValue = null;
        _callDepth--;
        return result;
    }

    private static object? Operate(NtslTokenType op, object? left, object? right, int line)
    {
        switch (op)
        {
            case NtslTokenType.Equal:
                return AreEqual(left, right) ? 1.0 : 0.0;
            case NtslTokenType.NotEqual:
                return AreEqual(left, right) ? 0.0 : 1.0;
            case NtslTokenType.Plus when left is NtslList list:
                return ListPlus(list, right, line);
            case NtslTokenType.Minus when left is NtslList list:
                return ListMinus(list, right);
            case NtslTokenType.Plus when left is string || right is string:
                return ToText(left) + ToText(right);
            case NtslTokenType.Less or NtslTokenType.LessEqual or NtslTokenType.Greater or NtslTokenType.GreaterEqual
                when left is string leftText && right is string rightText:
                return Compare(op, string.CompareOrdinal(leftText, rightText)) ? 1.0 : 0.0;
        }

        var a = ToNumber(left, line);
        var b = ToNumber(right, line);
        return op switch
        {
            NtslTokenType.Plus => a + b,
            NtslTokenType.Minus => a - b,
            NtslTokenType.Star => a * b,
            NtslTokenType.Slash => b != 0 ? a / b : throw new NtslException("ntsl-error-divide-by-zero", line),
            NtslTokenType.Percent => b != 0 ? a % b : throw new NtslException("ntsl-error-divide-by-zero", line),
            NtslTokenType.Caret => Math.Pow(a, b),
            _ => Compare(op, a.CompareTo(b)) ? 1.0 : 0.0,
        };
    }

    private static bool Compare(NtslTokenType op, int comparison)
    {
        return op switch
        {
            NtslTokenType.Less => comparison < 0,
            NtslTokenType.LessEqual => comparison <= 0,
            NtslTokenType.Greater => comparison > 0,
            _ => comparison >= 0,
        };
    }

    private static NtslList ListPlus(NtslList list, object? right, int line)
    {
        var result = list.Copy();
        if (right is NtslList other)
        {
            foreach (var item in other.Items)
            {
                AddItem(result, item, line);
            }
        }
        else
        {
            AddItem(result, right, line);
        }

        return result;
    }

    private static NtslList ListMinus(NtslList list, object? right)
    {
        var result = list.Copy();
        var toRemove = right is NtslList other ? other.Items : new List<object?> { right };
        foreach (var item in toRemove)
        {
            var index = result.Items.FindIndex(existing => AreEqual(existing, item));
            if (index < 0)
                continue;

            result.Items.RemoveAt(index);
            if (item is string key)
                result.Values.Remove(key);
        }

        return result;
    }

    private static object? GetMember(object? target, string name, int line)
    {
        return target switch
        {
            NtslList list when name == "len" => (double) list.Items.Count,
            string text when name == "len" => (double) text.Length,
            NtslList list => list.Get(name),
            _ => throw new NtslException("ntsl-error-no-member", line, ("name", name)),
        };
    }

    private static void SetMember(object? target, string name, object? value, int line)
    {
        if (target is not NtslList list || name == "len")
            throw new NtslException("ntsl-error-no-member", line, ("name", name));

        SetKey(list, name, value, line);
    }

    private static object? GetIndex(object? target, object? index, int line)
    {
        return (target, index) switch
        {
            (NtslList list, string key) => list.Get(key),
            (NtslList list, double number) => InRange(number, list.Items.Count) ? list.Items[(int) number - 1] : null,
            (string text, double number) => InRange(number, text.Length) ? text[(int) number - 1].ToString() : null,
            (NtslList or string, null) => null,
            _ => throw new NtslException("ntsl-error-bad-index", line),
        };
    }

    private static bool InRange(double number, int count)
    {
        return number >= 1 && number <= count;
    }

    private static void SetIndex(object? target, object? index, object? value, int line)
    {
        switch (target, index)
        {
            case (NtslList list, string key):
                SetKey(list, key, value, line);
                return;
            case (NtslList list, double number) when number == list.Items.Count + 1:
                AddItem(list, value, line);
                return;
            case (NtslList list, double number):
                list.Items[ToPosition(number, list.Items.Count, line)] = value;
                return;
            default:
                throw new NtslException("ntsl-error-bad-index", line);
        }
    }

    private static int ToPosition(double number, int count, int line)
    {
        if (double.IsNaN(number) || number < 1 || number > count)
            throw new NtslException("ntsl-error-index-out-of-range", line, ("index", number), ("count", count));

        return (int) number - 1;
    }

    private static void AddItem(NtslList list, object? value, int line)
    {
        if (list.Items.Count >= MaxListLength)
            throw new NtslException("ntsl-error-list-too-long", line, ("max", MaxListLength));

        list.Items.Add(value);
    }

    private static void SetKey(NtslList list, string key, object? value, int line)
    {
        if (!list.Values.ContainsKey(key) && list.Items.Count >= MaxListLength)
            throw new NtslException("ntsl-error-list-too-long", line, ("max", MaxListLength));

        list.Set(key, value);
    }

    private static string ToKey(object? value, int line)
    {
        return value as string ?? throw new NtslException("ntsl-error-bad-key", line);
    }

    private static bool AreEqual(object? a, object? b)
    {
        return (a, b) switch
        {
            (null, null) => true,
            (double x, double y) => x == y,
            (string x, string y) => x == y,
            _ => ReferenceEquals(a, b),
        };
    }

    public static bool IsTrue(object? value)
    {
        return value switch
        {
            null => false,
            double number => number != 0,
            string text => text.Length > 0,
            _ => true,
        };
    }

    public static double ToNumber(object? value, int line)
    {
        return value switch
        {
            null => 0,
            double number => number,
            _ => throw new NtslException("ntsl-error-not-a-number", line),
        };
    }

    public static string ToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            double number => Math.Round(number, 6).ToString(CultureInfo.InvariantCulture),
            string text => text,
            NtslList => "[list]",
            _ => "[function]",
        };
    }
}
