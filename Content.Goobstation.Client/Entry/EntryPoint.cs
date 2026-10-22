// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
// SPDX-FileCopyrightText: 2025 John Willis <143434770+CerberusWolfie@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Sara Aldrete's Top Guy <malchanceux@protonmail.com>
// SPDX-FileCopyrightText: 2025 Sara Aldrete's Top Guy <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Client.IoC;
using Content.Goobstation.Client.Polls;
using Content.Goobstation.Client.JoinQueue;
using Content.Goobstation.Common.ServerCurrency;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    [Dependency] private JoinQueueManager _joinQueue = default!;
    [Dependency] private PollManager _pollManager = default!;
    [Dependency] private ICommonCurrencyManager _currMan = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override void Init()
    {
        ContentGoobClientIoC.Register();

        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);

        _prototypeManager.RegisterIgnore("achievement");
    }

    public override void PostInit()
    {
        base.PostInit();

        _joinQueue.Initialize();
        _pollManager.Initialize();
        _currMan.Initialize();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _currMan.Shutdown();
    }
}
