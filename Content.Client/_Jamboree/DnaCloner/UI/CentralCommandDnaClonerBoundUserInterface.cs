// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
// SPDX-FileCopyrightText: 2026 PureBreadBagel
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.DnaCloner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client._Jamboree.DnaCloner.UI;

[UsedImplicitly]
public sealed class CentralCommandDnaClonerBoundUserInterface : BoundUserInterface
{
    private CentralCommandDnaClonerWindow? _window;

    public CentralCommandDnaClonerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CentralCommandDnaClonerWindow
        {
            Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName, // Set the title of the window to the MOLECULARSYNTHESIS machines name.
        };

        _window.OnRespawnRequested += mind => SendMessage(new CentralCommandDnaClonerRespawnMessage(mind)); //
        _window.OnClose += Close;
        _window.OpenCentered(); // just make it open to the centre of the clients screen.
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CentralCommandDnaClonerConsoleBoundUserInterfaceState castState)
            return;

        _window?.Populate(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();  // basically remove this from memory when the window is closed.
    }
}
