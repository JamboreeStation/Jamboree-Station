// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// Methods called on lists, like words.Find("hi") or words.Cut(1, 3). Names ignore case.
/// </summary>
public sealed partial class NtslInterpreter
{
    private static readonly HashSet<string> ListMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Find",
        "Join",
        "Copy",
        "Cut",
        "Add",
        "Remove",
        "Insert",
        "Swap",
    };

    private static bool IsListMethod(string name)
    {
        return ListMethods.Contains(name);
    }

    private static object? CallListMethod(NtslList list, string name, List<object?> args, int line)
    {
        switch (name.ToLowerInvariant())
        {
            case "find":
            {
                var needle = Arg(args, 0);
                return (double) (list.Items.FindIndex(item => AreEqual(item, needle)) + 1);
            }

            case "join":
                return string.Join(ToText(Arg(args, 0)), list.Items.Select(ToText));

            case "copy":
            {
                var (start, end) = GetRange(list, args, line);
                var copy = new NtslList();
                for (var i = start; i < end; i++)
                {
                    var item = list.Items[i];
                    copy.Items.Add(item);
                    if (item is string key && list.Values.TryGetValue(key, out var value))
                        copy.Values[key] = value;
                }

                return copy;
            }

            case "cut":
            {
                var (start, end) = GetRange(list, args, line);
                for (var i = end - 1; i >= start; i--)
                {
                    RemoveItemAt(list, i);
                }

                return 1.0;
            }

            case "add":
                foreach (var arg in args)
                {
                    if (arg is NtslList other)
                    {
                        foreach (var item in other.Items.ToList())
                        {
                            AddItem(list, item, line);
                        }
                    }
                    else
                    {
                        AddItem(list, arg, line);
                    }
                }

                return (double) list.Items.Count;

            case "remove":
            {
                var removed = 0.0;
                foreach (var arg in args)
                {
                    var index = list.Items.FindIndex(item => AreEqual(item, arg));
                    if (index < 0)
                        continue;

                    RemoveItemAt(list, index);
                    removed = 1.0;
                }

                return removed;
            }

            case "insert":
            {
                var position = ToNumber(Arg(args, 0), line);
                if (double.IsNaN(position) || position < 1 || position > list.Items.Count + 1)
                {
                    throw new NtslException("ntsl-error-index-out-of-range", line,
                        ("index", position),
                        ("count", list.Items.Count));
                }

                for (var i = 1; i < args.Count; i++)
                {
                    if (list.Items.Count >= MaxListLength)
                        throw new NtslException("ntsl-error-list-too-long", line, ("max", MaxListLength));

                    list.Items.Insert((int) position - 2 + i, args[i]);
                }

                return position;
            }

            case "swap":
            {
                var a = ToNumber(Arg(args, 0), line);
                var b = ToNumber(Arg(args, 1), line);
                if (!InRange(a, list.Items.Count) || !InRange(b, list.Items.Count))
                {
                    throw new NtslException("ntsl-error-index-out-of-range", line,
                        ("index", InRange(a, list.Items.Count) ? b : a),
                        ("count", list.Items.Count));
                }

                (list.Items[(int) a - 1], list.Items[(int) b - 1]) = (list.Items[(int) b - 1], list.Items[(int) a - 1]);
                return null;
            }

            default:
                return null;
        }
    }

    private static (int Start, int End) GetRange(NtslList list, List<object?> args, int line)
    {
        var count = list.Items.Count;
        var start = args.Count > 0 && args[0] != null ? (int) ToNumber(args[0], line) : 1;
        var end = args.Count > 1 && args[1] != null ? (int) ToNumber(args[1], line) : 0;

        start = Math.Clamp(start, 1, count + 1);
        if (end <= 0 || end > count + 1)
            end = count + 1;

        if (end < start)
            end = start;

        return (start - 1, end - 1);
    }

    private static void RemoveItemAt(NtslList list, int index)
    {
        var item = list.Items[index];
        list.Items.RemoveAt(index);

        if (item is string key && !list.Items.Contains(key))
            list.Values.Remove(key);
    }
}
