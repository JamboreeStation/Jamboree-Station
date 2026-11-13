// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.CustomRadio;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.CustomRadio;

public sealed class CustomEncryptionKeyBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CustomEncryptionKeyWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CustomEncryptionKeyWindow>();
        _window.OnConfirm += (name, key, color) => SendMessage(new CustomEncryptionKeySetupMessage(name, key, color));
    }
}