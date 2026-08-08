using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Messaging;

/// <summary>
/// Messaging — lifecycle subscriptions (page-held) vs weak-reference GC probe.
/// </summary>
[DotPudicaView(typeof(MessagingPageViewModel))]
public partial class MessagingPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(MessagingPageViewModel.GuideText))]
    private Label _guideLabel = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.LastSendText))]
    private Label _lastSendLabel = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.OwnedSectionTitle))]
    private Label _ownedSectionTitle = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.BusRegisterText))]
    private Label _busLabel = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.ViewModelRegisterText))]
    private Label _vmLabel = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.OwnedFooterText))]
    private Label _ownedFooterLabel = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.WeakSectionTitle))]
    private Label _weakSectionTitle = null!;

    [Export, BindTo(nameof(MessagingPageViewModel.SubscriberStatusText))]
    private Label _subscriberStatusLabel = null!;

    [Export, BindCommand(nameof(MessagingPageViewModel.SendMessageCommand))]
    private Button _sendMessageButton = null!;

    [Export, BindCommand(nameof(MessagingPageViewModel.CreateSubscriberCommand))]
    private Button _createSubscriberButton = null!;

    [Export, BindCommand(nameof(MessagingPageViewModel.ForceGcCommand))]
    private Button _forceGcButton = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Lifecycle subscriptions vs weak GC probe.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Last Send", out _lastSendLabel);

        var actions = ShowcaseUi.AddActionRow(root);
        _sendMessageButton = ShowcaseUi.CreatePrimaryButton("Send Ping");
        _createSubscriberButton = ShowcaseUi.CreateActionButton("Create Weak Subscriber");
        _forceGcButton = ShowcaseUi.CreateActionButton("Force GC");
        actions.AddChild(_sendMessageButton);
        actions.AddChild(_createSubscriberButton);
        actions.AddChild(_forceGcButton);

        _guideLabel = new Label { Visible = false };
        root.AddChild(_guideLabel);

        ShowcaseUi.AddSection(root, "Owned");
        var ownedBody = ShowcaseUi.CreateCardBody(out var ownedPanel);
        _ownedSectionTitle = new Label { Modulate = ShowcaseTheme.Muted };
        _busLabel = new Label();
        _vmLabel = new Label();
        _ownedFooterLabel = new Label { Modulate = ShowcaseTheme.Muted };
        ownedBody.AddChild(_ownedSectionTitle);
        ownedBody.AddChild(_busLabel);
        ownedBody.AddChild(_vmLabel);
        ownedBody.AddChild(_ownedFooterLabel);
        root.AddChild(ownedPanel);

        ShowcaseUi.AddSection(root, "Weak");
        var weakBody = ShowcaseUi.CreateCardBody(out var weakPanel);
        _weakSectionTitle = new Label { Modulate = ShowcaseTheme.Muted };
        _subscriberStatusLabel = new Label();
        weakBody.AddChild(_weakSectionTitle);
        weakBody.AddChild(_subscriberStatusLabel);
        root.AddChild(weakPanel);
    }
}
