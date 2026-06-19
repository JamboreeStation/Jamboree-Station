// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Medical.Components;
using Content.Shared._Jamboree.Mutations.GeneticMachines;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Server._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Keeps a <see cref="MedicalScannerConsoleComponent"/> console's underlying
///     <see cref="HealthAnalyzerComponent"/> pointing at whoever is currently sealed
///     inside its linked medical scanner. The engine's HealthAnalyzerSystem then
///     drives the regular HealthAnalyzer UI tick for free.
/// </summary>
public sealed class MedicalScannerConsoleSystem : EntitySystem
{
    [Dependency] private readonly GeneticMachineSystem _machine = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedicalScannerConsoleComponent, AfterActivatableUIOpenEvent>((u, _, _) => RefreshLink(u));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MedicalScannerConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
            RefreshLink(uid);
    }

    private void RefreshLink(EntityUid console)
    {
        if (!TryComp<HealthAnalyzerComponent>(console, out var analyzer))
            return;
        if (!TryComp<GeneticMachineComponent>(console, out var machine))
            return;

        _machine.RecheckRange(console, machine);
        var occupant = _machine.GetScannedBody(console, machine);

        if (analyzer.ScannedEntity == occupant)
            return;

        analyzer.ScannedEntity = occupant;
        // Force HealthAnalyzerSystem.Update to push fresh state next tick.
        analyzer.NextUpdate = _timing.CurTime;
    }
}
