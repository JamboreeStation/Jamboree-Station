// SPDX-FileCopyrightText: 2019 Space Station 14 Contributors
// SPDX-License-Identifier: MIT
// Adapted from RobustToolbox 56eda3ea92913e60c27aa029ba7877c8559dfe9d.

// Compatibility validators for unchanged Jamboree string prototype fields.
using System.Collections.Immutable;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public partial class PrototypeIdListSerializer<T> :
        ITypeValidator<ImmutableList<string>, SequenceDataNode>
        where T : class, IPrototype
    {
        public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return ValidateInternal(serializationManager, node, dependencies, context);
        }
    }
}
