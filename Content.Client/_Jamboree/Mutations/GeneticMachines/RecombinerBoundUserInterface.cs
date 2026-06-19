// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.Mutations.GeneticMachines;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.Mutations.GeneticMachines;

[UsedImplicitly]
public sealed class RecombinerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RecombinerWindow? _window;

    public RecombinerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RecombinerWindow>();
        _window.OnPackToggleClicked += packEnt => SendMessage(new RecombinerTogglePackMessage(packEnt));
        _window.OnEjectClicked += packEnt => SendMessage(new RecombinerEjectMessage(packEnt));
        _window.StartButton.OnPressed += _ => SendMessage(new RecombinerStartMessage());
        _window.GenotypeInput.OnTextEntered += args =>
            SendMessage(new RecombinerSetGenotypeNameMessage(args.Text));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is RecombinerBoundUserInterfaceState s)
            _window?.Populate(s);
    }
}
