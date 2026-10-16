// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.CustomRadio;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.CustomRadio;

public sealed class CustomRadioServerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CustomRadioServerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CustomRadioServerWindow>();
        _window.OnSave += (id, name, key, color) => SendMessage(new CustomRadioServerEditMessage(id, name, key, color));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CustomRadioServerBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}