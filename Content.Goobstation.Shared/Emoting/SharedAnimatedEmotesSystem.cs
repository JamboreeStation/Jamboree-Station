// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Emoting;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Emoting;

public abstract partial class SharedAnimatedEmotesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(Entity<AnimatedEmotesComponent> ent, ref ComponentGetState args)
    {
        args.State = new AnimatedEmotesComponentState(ent.Comp.Emote);
    }
}
