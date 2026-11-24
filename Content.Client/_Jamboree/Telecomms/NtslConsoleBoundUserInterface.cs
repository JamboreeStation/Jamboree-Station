// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Jamboree.Telecomms;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.Telecomms;

public sealed class NtslConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private NtslConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NtslConsoleWindow>();
        _window.OnServerSelected += server => SendMessage(new NtslConsoleSelectServerMessage(server));
        _window.OnUpload += code => SendMessage(new NtslConsoleUploadMessage(code));
        _window.OnClear += () => SendMessage(new NtslConsoleClearMessage());
        _window.OnRestore += () => SendMessage(new NtslConsoleRestoreMessage());
        _window.OnEnabledChanged += enabled => SendMessage(new NtslConsoleSetEnabledMessage(enabled));
        _window.OnRefresh += () => SendMessage(new NtslConsoleRefreshMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is NtslConsoleBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
