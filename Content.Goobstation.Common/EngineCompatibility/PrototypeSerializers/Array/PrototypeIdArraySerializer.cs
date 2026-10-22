// SPDX-FileCopyrightText: 2019 Space Station 14 Contributors
// SPDX-License-Identifier: MIT
// Adapted from RobustToolbox 56eda3ea92913e60c27aa029ba7877c8559dfe9d.

// Compatibility validators for unchanged Jamboree string prototype fields.
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

[Virtual]
public class PrototypeIdArraySerializer<TPrototype> : ITypeValidator<string[], SequenceDataNode>,
    ITypeValidator<string[], ValueDataNode> where TPrototype : class, IPrototype
{
    protected virtual PrototypeIdSerializer<TPrototype> PrototypeSerializer => new();

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedSequenceNode(node.Select(x =>
            x is ValueDataNode valueDataNode
                ? PrototypeSerializer.Validate(serializationManager, valueDataNode, dependencies, context)
                : new ErrorNode(x, $"Cannot cast node {x} to ValueDataNode.")).ToList());
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null) =>
        PrototypeSerializer.Validate(serializationManager, node, dependencies, context);
}

