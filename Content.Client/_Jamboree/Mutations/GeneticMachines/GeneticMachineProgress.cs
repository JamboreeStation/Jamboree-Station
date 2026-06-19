using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Jamboree.Mutations.GeneticMachines;

/// <summary>
///     Shared progress-bar update for every genetic-machine window. Derives the
///     current 0..1 progress from server-supplied StartedAt/FinishesAt timestamps
///     plus the client's local game time.
/// </summary>
internal static class GeneticMachineProgress
{
    public static void Update(IGameTiming timing, ProgressBar bar, TimeSpan? startedAt, TimeSpan? finishesAt)
    {
        if (startedAt is not { } start || finishesAt is not { } end || end <= start)
        {
            bar.Visible = false;
            bar.Value = 0f;
            return;
        }

        var total = (end - start).TotalSeconds;
        var elapsed = (timing.CurTime - start).TotalSeconds;
        var t = total <= 0 ? 1f : (float) Math.Clamp(elapsed / total, 0, 1);
        bar.Value = t;
        bar.Visible = true;
    }
}
