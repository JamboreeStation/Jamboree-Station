// SPDX-License-Identifier: MIT
using System.Globalization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.Paper.UI;

/// <summary>
/// Preserves paper's historical left, bottom, right, top margin format without using a geometric box.
/// </summary>
public sealed class PaperMarginSerializer : ITypeSerializer<Thickness, ValueDataNode>
{
    public Thickness Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Thickness>? instanceProvider = null)
    {
        var values = node.Value.Split(',');
        return new Thickness(float.Parse(values[0], CultureInfo.InvariantCulture),
            float.Parse(values[3], CultureInfo.InvariantCulture),
            float.Parse(values[2], CultureInfo.InvariantCulture),
            float.Parse(values[1], CultureInfo.InvariantCulture));
    }

    public DataNode Write(ISerializationManager serializationManager, Thickness value,
        IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
        => new ValueDataNode(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}",
            value.Left, value.Bottom, value.Right, value.Top));

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var values = node.Value.Split(',');
        return values.Length == 4 && values.All(value => float.TryParse(value,
            NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Expected four margin values: left, bottom, right, top.");
    }
}
