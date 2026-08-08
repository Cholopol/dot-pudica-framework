using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Threading;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.MiniGame.Loadout;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.MiniGame.Lobby;

/// <summary>Lobby page: room heartbeat mailbox + ItemsSource list.</summary>
[DotPudicaView(typeof(LobbyViewModel))]
public partial class LobbyPage : ShowcasePageWindow
{
    private const string ItemScenePath = "res://samples/Showcase/MiniGame/Lobby/LobbyRoomItem.tscn";

    [Inject]
    private IRoomService _roomService = null!;

    [Export, BindTo(nameof(LobbyViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, ItemsSource("Rooms", ItemScenePath, PoolSize = 16)]
    private VBoxContainer _roomList = null!;

    [Export, BindCommand(nameof(LobbyViewModel.EnterLoadoutCommand))]
    private Button _enterLoadoutButton = null!;

    [ViewModelFactory]
    private LobbyViewModel CreateLobbyViewModel()
    {
        var dispatcher = UiDispatcher.FromSynchronizationContext(
            Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext"));
        return new LobbyViewModel(_roomService, dispatcher);
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewModelBound()
    {
        _roomService.Start(hertz: 10);
    }

    partial void OnViewDisposing()
    {
        _roomService.Stop();
    }

    public override void _Process(double delta)
    {
        ViewModel?.DrainOnUiThread();
    }

    [Subscribe(nameof(LobbyViewModel.EnterLoadoutRequested))]
    private void OnEnterLoadoutRequested()
    {
        var loadout = new LoadoutPage { WindowName = "Loadout" };
        ShowFullPage(loadout);
        RequireWindowManager().Dismiss(this);
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Room heartbeat + snapshot mailbox.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        root.AddChild(scroll);

        _roomList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _roomList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_roomList);

        var actionRow = ShowcaseUi.AddActionRow(root);
        _enterLoadoutButton = ShowcaseUi.CreatePrimaryButton("Enter Loadout");
        actionRow.AddChild(_enterLoadoutButton);
    }
}
