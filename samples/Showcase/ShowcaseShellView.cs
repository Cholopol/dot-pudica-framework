using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.Gallery.BindingBasics;
using Samples.Showcase.Gallery.Collections;
using Samples.Showcase.Gallery.Commands;
using Samples.Showcase.Gallery.Diagnostics;
using Samples.Showcase.Gallery.Messaging;
using Samples.Showcase.Gallery.Pools;
using Samples.Showcase.Gallery.ScopesAndDi;
using Samples.Showcase.Gallery.ThreadingLab;
using Samples.Showcase.Gallery.Validation;
using Samples.Showcase.Gallery.VirtualList;
using Samples.Showcase.Gallery.Windows;
using Samples.Showcase.MiniGame.Login;
using AppContext = DotPudica.Godot.AppContext;

namespace Samples.Showcase;

/// <summary>
/// Showcase shell: fixed top banner + left nav + right stage (LabHud / ContentHost / OverlayHost).
/// </summary>
public partial class ShowcaseShellView : Control
{
    private VBoxContainer _navList = null!;
    private Label _statusLabel = null!;
    private Label _bannerTitle = null!;
    private Control _contentHost = null!;
    private Control _overlayHost = null!;
    private ShowcaseLabHud _labHud = null!;
    private GodotWindow? _currentPage;
    private readonly Dictionary<string, Func<GodotWindow>> _factories = new();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var column = new VBoxContainer
        {
            Name = "ShellColumn",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 0);
        AddChild(column);

        column.AddChild(ShowcaseUi.CreateTopBanner(out _bannerTitle));

        var root = new HBoxContainer
        {
            Name = "Root",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 0);
        column.AddChild(root);

        var left = new PanelContainer
        {
            Name = "NavPanel",
            CustomMinimumSize = new Vector2(ShowcaseTheme.NavWidth, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        left.AddThemeStyleboxOverride("panel", ShowcaseTheme.NavStyle());
        root.AddChild(left);

        var leftInner = new VBoxContainer
        {
            Name = "Nav",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftInner.AddThemeConstantOverride("separation", 8);
        var leftPad = new MarginContainer();
        leftPad.AddThemeConstantOverride("margin_left", 12);
        leftPad.AddThemeConstantOverride("margin_top", 14);
        leftPad.AddThemeConstantOverride("margin_right", 12);
        leftPad.AddThemeConstantOverride("margin_bottom", 12);
        left.AddChild(leftPad);
        leftPad.AddChild(leftInner);

        leftInner.AddChild(new Label
        {
            Text = "Capability Gallery",
            Modulate = ShowcaseTheme.Muted
        });

        leftInner.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        leftInner.AddChild(scroll);

        _navList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        _navList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_navList);

        _statusLabel = new Label
        {
            Text = "Ready",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        leftInner.AddChild(_statusLabel);

        var right = new VBoxContainer
        {
            Name = "RightColumn",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        right.AddThemeConstantOverride("separation", 0);
        root.AddChild(right);

        _labHud = new ShowcaseLabHud();
        right.AddChild(_labHud);
        ShowcaseLayout.LabHud = _labHud;

        var stage = new Control
        {
            Name = "Stage",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ClipContents = true
        };
        right.AddChild(stage);

        _contentHost = new Control
        {
            Name = "ContentHost",
            ClipContents = true
        };
        _contentHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var contentBg = new ColorRect
        {
            Name = "ContentBackground",
            Color = ShowcaseTheme.ContentBg,
            MouseFilter = MouseFilterEnum.Ignore
        };
        contentBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentHost.AddChild(contentBg);
        stage.AddChild(_contentHost);
        ShowcaseLayout.ContentHost = _contentHost;

        _overlayHost = new Control
        {
            Name = "OverlayHost",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _overlayHost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        stage.AddChild(_overlayHost);
        ShowcaseLayout.OverlayHost = _overlayHost;

        var placeholder = new Label
        {
            Name = "Placeholder",
            Text = "Select a page from the left",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = ShowcaseTheme.Muted
        };
        placeholder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentHost.AddChild(placeholder);

        RegisterPages();
        BuildNavButtons();
        ShowcaseLayout.BannerTitleSetter = SetBannerTitle;
        Navigate("BindingBasics");
    }

    public override void _ExitTree()
    {
        if (ShowcaseLayout.BannerTitleSetter == SetBannerTitle)
            ShowcaseLayout.BannerTitleSetter = null;
        if (ReferenceEquals(ShowcaseLayout.ContentHost, _contentHost))
            ShowcaseLayout.ContentHost = null;
        if (ReferenceEquals(ShowcaseLayout.OverlayHost, _overlayHost))
            ShowcaseLayout.OverlayHost = null;
        if (ReferenceEquals(ShowcaseLayout.LabHud, _labHud))
            ShowcaseLayout.LabHud = null;
        base._ExitTree();
    }

    private void SetBannerTitle(string title) => _bannerTitle.Text = title;

    private void RegisterPages()
    {
        _factories["BindingBasics"] = () => new BindingBasicsPage();
        _factories["Commands"] = () => new CommandsPage();
        _factories["Validation"] = () => new ValidationPage();
        _factories["Collections"] = () => new CollectionsPage();
        _factories["VirtualList"] = () => new VirtualListPage();
        _factories["ThreadingLab"] = () => new ThreadingLabPage();
        _factories["ScopesAndDi"] = () => new ScopesAndDiPage();
        _factories["Windows"] = () => new WindowsPage();
        _factories["Messaging"] = () => new MessagingPage();
        _factories["Pools"] = () => new PoolsPage();
        _factories["Diagnostics"] = () => new DiagnosticsPage();
        _factories["MiniGame"] = () => new LoginPage();
    }

    private void BuildNavButtons()
    {
        AddSection("GALLERY");
        foreach (var key in new[]
                 {
                     "BindingBasics", "Commands", "Validation", "Collections", "VirtualList",
                     "ThreadingLab", "ScopesAndDi", "Windows", "Messaging", "Pools", "Diagnostics"
                 })
            AddNavButton(key, key);

        AddSection("MINIGAME");
        AddNavButton("MiniGame", "Online Match");
    }

    private void AddSection(string title)
    {
        _navList.AddChild(new Label
        {
            Text = title,
            Modulate = ShowcaseTheme.Section
        });
    }

    private void AddNavButton(string key, string label)
    {
        var btn = ShowcaseUi.CreateActionButton(label, expand: true);
        btn.CustomMinimumSize = new Vector2(0, 30);
        btn.Pressed += () => Navigate(key);
        _navList.AddChild(btn);
    }

    public void Navigate(string key)
    {
        if (!_factories.TryGetValue(key, out var factory))
        {
            _statusLabel.Text = $"Unknown: {key}";
            return;
        }

        var wm = AppContext.Current.WindowManager;
        if (_currentPage is { Dismissed: false })
            wm.Dismiss(_currentPage, ignoreAnimation: true);

        var placeholder = _contentHost.GetNodeOrNull("Placeholder");
        placeholder?.QueueFree();

        if (key == "Windows")
            _labHud.ShowForWindowsLab();
        else
            _labHud.HideAndReset();

        var page = factory();
        if (string.IsNullOrEmpty(page.WindowName))
            page.WindowName = key;
        ShowcaseLayout.PrepareFullPage(page);
        wm.Show(page);
        _currentPage = page;
        ShowcaseLayout.SetBannerInstance(page.WindowName);
        _statusLabel.Text = key;
    }
}
