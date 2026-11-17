// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Jamboree.Ntsl;

/// <summary>
/// The NTSL list: ordered items and optional values for each string keys.
/// </summary>
public sealed class NtslList
{
    public readonly List<object?> Items = new();
    public readonly Dictionary<string, object?> Values = new();

    public object? Get(string key)
    {
        return Values.GetValueOrDefault(key);
    }

    public void Set(string key, object? value)
    {
        if (!Values.ContainsKey(key))
            Items.Add(key);

        Values[key] = value;
    }

    public NtslList Copy()
    {
        var copy = new NtslList();
        copy.Items.AddRange(Items);
        foreach (var (key, value) in Values)
        {
            copy.Values[key] = value;
        }

        return copy;
    }
}

/// <summary>
/// So a function written in NTSL with def, remembering the scope it was written in.
/// </summary>
public sealed class NtslFunction(NtslDefStmt def, NtslScope closure)
{
    public NtslDefStmt Def { get; } = def;
    public NtslScope Closure { get; } = closure;
}

/// <summary>
/// A function that scripts can call like upper() or broadcast().
/// </summary>
public delegate object? NtslBuiltin(List<object?> args, int line);

/// <summary>
/// The variables that are visible at one level: built-ins, scripts globals or func calls.
/// </summary>
public sealed class NtslScope(NtslScope? parent)
{
    private readonly NtslScope? _parent = parent;
    private readonly Dictionary<string, object?> _variables = new();

    public bool TryGet(string name, out object? value)
    {
        for (var scope = this; scope != null; scope = scope._parent)
        {
            if (scope._variables.TryGetValue(name, out value))
                return true;
        }

        value = null;
        return false;
    }

    public void Assign(string name, object? value)
    {
        for (var scope = this; scope != null; scope = scope._parent)
        {
            if (!scope._variables.ContainsKey(name))
                continue;

            scope._variables[name] = value;
            return;
        }

        _variables[name] = value;
    }

    public void Declare(string name, object? value)
    {
        _variables[name] = value;
    }
}