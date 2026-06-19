using Content.Shared._Jamboree.Mutations.GeneticMachines;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Jamboree.Mutations.GeneticMachines;

[UsedImplicitly]
public sealed class EvolutionizerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EvolutionizerWindow? _window;

    public EvolutionizerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<EvolutionizerWindow>();
        _window.ActivateButton.OnPressed += _ => SendMessage(new EvolutionizerActivateMessage());
        _window.DnaSetButton.OnPressed += _ => SendMessage(new EvolutionizerSetDnaMessage(_window.DnaInput.Text));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is EvolutionizerBoundUserInterfaceState s)
            _window?.Populate(s);
    }
}
