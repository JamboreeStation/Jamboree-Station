// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Linq;
using System.Text;

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// The functions every NTSL script can use, whatever device runs it.
/// </summary>
public sealed partial class NtslInterpreter
{
    private readonly Random _random = new();

    private void RegisterCoreBuiltins()
    {
        _builtins.Declare("true", 1.0);
        _builtins.Declare("TRUE", 1.0);
        _builtins.Declare("false", 0.0);
        _builtins.Declare("FALSE", 0.0);
        _builtins.Declare("null", null);
        _builtins.Declare("PI", Math.PI);
        _builtins.Declare("E", Math.E);
        _builtins.Declare("SQRT2", Math.Sqrt(2));
        _builtins.Declare("SQURT2", Math.Sqrt(2));
        _builtins.Declare("NORTH", 1.0);
        _builtins.Declare("SOUTH", 2.0);
        _builtins.Declare("EAST", 4.0);
        _builtins.Declare("WEST", 8.0);

        Register("length", Length);
        Register("lower", (args, _) => ToText(Arg(args, 0)).ToLowerInvariant());
        Register("upper", (args, _) => ToText(Arg(args, 0)).ToUpperInvariant());
        Register("tostring", (args, _) => ToText(Arg(args, 0)));
        Register("tonum", ToNum);
        Register("find", Find);
        Register("substr", Substr);
        Register("replace", Replace);
        Register("explode", Explode);
        Register("implode", Implode);
        Register("repeat", Repeat);
        Register("reverse", Reverse);
        Register("proper", Proper);
        Register("trim", (args, _) => ToText(Arg(args, 0)).Trim());

        Register("abs", (args, line) => Math.Abs(ToNumber(Arg(args, 0), line)));
        Register("round", (args, line) => Math.Round(ToNumber(Arg(args, 0), line), MidpointRounding.AwayFromZero));
        Register("floor", (args, line) => Math.Floor(ToNumber(Arg(args, 0), line)));
        Register("ceil", (args, line) => Math.Ceiling(ToNumber(Arg(args, 0), line)));
        Register("sqrt", (args, line) => Math.Sqrt(ToNumber(Arg(args, 0), line)));
        Register("min", (args, line) => Extreme(args, line, Math.Min));
        Register("max", (args, line) => Extreme(args, line, Math.Max));
        Register("log", Log);
        Register("sin", (args, line) => Math.Sin(ToRadians(Arg(args, 0), line)));
        Register("cos", (args, line) => Math.Cos(ToRadians(Arg(args, 0), line)));
        Register("tan", (args, line) => Math.Tan(ToRadians(Arg(args, 0), line)));
        Register("asin", (args, line) => Math.Asin(ToNumber(Arg(args, 0), line)) * 180 / Math.PI);
        Register("acos", (args, line) => Math.Acos(ToNumber(Arg(args, 0), line)) * 180 / Math.PI);
        Register("atan", (args, line) => Math.Atan(ToNumber(Arg(args, 0), line)) * 180 / Math.PI);
        Register("clamp", Clamp);
        Register("inrange", InRangeBuiltin);

        Register("prob", (args, line) => _random.NextDouble() * 100 < ToNumber(Arg(args, 0), line) ? 1.0 : 0.0);
        Register("rand", Rand);
        Register("pick", Pick);
    }

    private static object? Arg(List<object?> args, int index)
    {
        return index < args.Count ? args[index] : null;
    }

    private static object? Length(List<object?> args, int line)
    {
        return Arg(args, 0) switch
        {
            string text => (double) text.Length,
            NtslList list => (double) list.Items.Count,
            _ => 0.0,
        };
    }

    private static object? ToNum(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    private static object? Find(List<object?> args, int line)
    {
        var needle = Arg(args, 1);
        if (Arg(args, 0) is NtslList list)
            return (double) (list.Items.FindIndex(item => AreEqual(item, needle)) + 1);

        var haystack = ToText(Arg(args, 0));
        return (double) (haystack.IndexOf(ToText(needle), StringComparison.OrdinalIgnoreCase) + 1);
    }

    private static object? Substr(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        var start = args.Count > 1 ? (int) ToNumber(args[1], line) : 1;
        var end = args.Count > 2 ? (int) ToNumber(args[2], line) : 0;

        start = Math.Clamp(start, 1, text.Length + 1);
        if (end <= 0 || end > text.Length + 1)
            end = text.Length + 1;

        return end <= start ? string.Empty : text[(start - 1)..(end - 1)];
    }

    private static object? Replace(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        var find = ToText(Arg(args, 1));
        if (find.Length == 0)
            return text;

        return text.Replace(find, ToText(Arg(args, 2)), StringComparison.OrdinalIgnoreCase);
    }

    private static object? Explode(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        var separator = ToText(Arg(args, 1));
        var parts = separator.Length == 0
            ? text.Select(c => c.ToString())
            : text.Split(separator);

        var list = new NtslList();
        list.Items.AddRange(parts.Take(MaxListLength));
        return list;
    }

    private static object? Implode(List<object?> args, int line)
    {
        if (Arg(args, 0) is not NtslList list)
            throw new NtslException("ntsl-error-not-a-list", line);

        return string.Join(ToText(Arg(args, 1)), list.Items.Select(ToText));
    }

    private static object? Repeat(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        var times = Math.Clamp(ToNumber(Arg(args, 1), line), 0, MaxStringLength);
        if (text.Length == 0)
            return string.Empty;

        var result = new StringBuilder();
        for (var i = 0; i < times && result.Length < MaxStringLength; i++)
        {
            result.Append(text);
        }

        return result.ToString();
    }

    private static object? Reverse(List<object?> args, int line)
    {
        if (Arg(args, 0) is NtslList list)
        {
            var reversed = list.Copy();
            reversed.Items.Reverse();
            return reversed;
        }

        var chars = ToText(Arg(args, 0)).ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    private static object? Proper(List<object?> args, int line)
    {
        var text = ToText(Arg(args, 0));
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static object? Log(List<object?> args, int line)
    {
        var value = ToNumber(Arg(args, 0), line);
        return args.Count > 1 && args[1] != null
            ? Math.Log(value, ToNumber(args[1], line))
            : Math.Log(value);
    }

    private static double ToRadians(object? value, int line)
    {
        return ToNumber(value, line) * Math.PI / 180;
    }

    private static (double Value, double Low, double High) GetBounds(List<object?> args, int line)
    {
        var value = ToNumber(Arg(args, 0), line);
        var low = args.Count > 1 && args[1] != null ? ToNumber(args[1], line) : -1;
        var high = args.Count > 2 && args[2] != null ? ToNumber(args[2], line) : 1;
        return low <= high ? (value, low, high) : (value, high, low);
    }

    private static object? Clamp(List<object?> args, int line)
    {
        var (value, low, high) = GetBounds(args, line);
        return Math.Clamp(value, low, high);
    }

    private static object? InRangeBuiltin(List<object?> args, int line)
    {
        var (value, low, high) = GetBounds(args, line);
        return value >= low && value <= high ? 1.0 : 0.0;
    }

    private static object? Extreme(List<object?> args, int line, Func<double, double, double> choose)
    {
        var values = args.Count == 1 && args[0] is NtslList list ? list.Items : args;
        if (values.Count == 0)
            return null;

        var result = ToNumber(values[0], line);
        for (var i = 1; i < values.Count; i++)
        {
            result = choose(result, ToNumber(values[i], line));
        }

        return result;
    }

    private object? Rand(List<object?> args, int line)
    {
        if (args.Count == 0)
            return _random.NextDouble();

        var low = args.Count == 1 ? 0 : (long) ToNumber(args[0], line);
        var high = (long) ToNumber(args[args.Count == 1 ? 0 : 1], line);
        if (high < low)
            (low, high) = (high, low);

        return (double) _random.NextInt64(low, high + 1);
    }

    private object? Pick(List<object?> args, int line)
    {
        var pool = new List<object?>();
        foreach (var arg in args)
        {
            if (arg is NtslList list)
                pool.AddRange(list.Items);
            else
                pool.Add(arg);
        }

        return pool.Count == 0 ? null : pool[_random.Next(pool.Count)];
    }
}
