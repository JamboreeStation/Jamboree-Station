// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 AndresE55 <80334192+leander-0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 JamboreeBot <JamboreeBot@proton.me>
// SPDX-FileCopyrightText: 2025 Lumminal <81829924+Lumminal@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Crayon;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<Content.Shared.Crayon.CrayonComponent>(ent => new StatusControl(ent, _charges, _entityManager));
    }

    private sealed class StatusControl : Control
    {
        private readonly Entity<Content.Shared.Crayon.CrayonComponent> _crayon;
        private readonly SharedChargesSystem _charges;
        private readonly RichTextLabel _label;
        private readonly EntityManager _entityManager;
        private string? _lastMarkup;

        public StatusControl(Entity<Content.Shared.Crayon.CrayonComponent> crayon, SharedChargesSystem charges, EntityManager entityManage)
        {
            _crayon = crayon;
            _charges = charges;
            _entityManager = entityManage;
            _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            var unlimited = !_entityManager.TryGetComponent<LimitedChargesComponent>(_crayon.Owner, out var charges)
                || charges.MaxCharges == int.MaxValue;
            var markup = unlimited
                ? Robust.Shared.Localization.Loc.GetString("crayon-drawing-label-unlimited",
                    ("color", _crayon.Comp.Color),
                    ("state", _crayon.Comp.SelectedState))
                : Robust.Shared.Localization.Loc.GetString("crayon-drawing-label",
                    ("color", _crayon.Comp.Color),
                    ("state", _crayon.Comp.SelectedState),
                    ("charges", _charges.GetCurrentCharges(_crayon.Owner)),
                    ("capacity", charges!.MaxCharges));

            if (_lastMarkup == markup)
                return;

            _lastMarkup = markup;
            _label.SetMarkup(markup);
        }
    }
}
