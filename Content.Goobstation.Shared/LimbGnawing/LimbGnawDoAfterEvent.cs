// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.LimbGnawing;

[Serializable, NetSerializable]
public sealed partial class LimbGnawDoAfterEvent : SimpleDoAfterEvent;
