// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.Mutations.GeneticMachines;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.Mutations.GeneticMachines;

[UsedImplicitly]
public sealed class MutatorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MutatorWindow? _window;

    public MutatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MutatorWindow>();
        _window.ActivateButton.OnPressed += _ => SendMessage(new MutatorActivateMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is MutatorBoundUserInterfaceState s)
            _window?.Populate(s);
    }
}
