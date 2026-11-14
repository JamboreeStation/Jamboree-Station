// SPDX-FileCopyrightText: 2025-2026 jhrushbe (Far-Horizons-SS14)
// SPDX-FileCopyrightText: The Goonstation Project (nuclear reactor code derivation, per the Far-Horizons-SS14 README)
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NuclearReactorMonitorComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public NetEntity? reactor;

    [DataField]
    public ProtoId<SinkPortPrototype> LinkingPort = "NuclearReactorDataReceiver";
}