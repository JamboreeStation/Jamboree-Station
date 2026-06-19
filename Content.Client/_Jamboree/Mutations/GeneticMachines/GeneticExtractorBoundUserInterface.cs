using Content.Shared._Jamboree.Mutations.GeneticMachines;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.Mutations.GeneticMachines;

[UsedImplicitly]
public sealed class GeneticExtractorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneticExtractorWindow? _window;

    public GeneticExtractorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GeneticExtractorWindow>();
        _window.CleanseModeButton.OnPressed += _ => SendMessage(new GeneticExtractorSetModeMessage(GeneticExtractorMode.Cleanse));
        _window.IsolateModeButton.OnPressed += _ => SendMessage(new GeneticExtractorSetModeMessage(GeneticExtractorMode.Isolate));
        _window.ActivateButton.OnPressed += _ => SendMessage(new GeneticExtractorActivateMessage());
        _window.OnIsolatedGeneSelected += id => SendMessage(new GeneticExtractorSetIsolatedMessage(id));
        _window.OnGenepackNameChanged += name => SendMessage(new GeneticExtractorSetGenepackNameMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is GeneticExtractorBoundUserInterfaceState s)
            _window?.Populate(s);
    }
}
