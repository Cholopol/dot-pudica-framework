using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Samples.Showcase.Gallery.ScopesAndDi;
using Samples.Showcase.Shared.Services;
using AppContext = DotPudica.Godot.AppContext;

namespace Samples.Showcase;

/// <summary>
/// Showcase bootstrap: initialize AppContext before any SceneContextHost enters the tree,
/// and ensure GodotWindowManager exists for DI + navigation.
/// </summary>
public partial class ShowcaseBootstrap : Node
{
    private AppContext? _app;
    private GodotWindowManager? _windowManager;

    public GodotWindowManager WindowManager => _windowManager
        ?? throw new InvalidOperationException("ShowcaseBootstrap is not in the tree yet.");

    public override void _EnterTree()
    {
        _windowManager = EnsureWindowManager();

        _app = new AppContext().Initialize(services =>
        {
            services.AddSingleton<IProfileService, FakeProfileService>();
            services.AddSingleton<IRoomService, FakeRoomService>();
            services.AddSingleton<IInventoryService, FakeInventoryService>();
            services.AddSingleton<IShowcaseMatchService>(_ =>
                new FakeShowcaseMatchService { Delay = TimeSpan.FromSeconds(2) });
            services.AddTransient<InjectedDemoViewModel>();
        }, _windowManager);

        base._EnterTree();
    }

    private GodotWindowManager EnsureWindowManager()
    {
        GodotWindowManager wm;
        var existing = GetNodeOrNull<GodotWindowManager>("WindowManager");
        if (existing is not null)
        {
            wm = existing;
        }
        else
        {
            var placeholder = GetNodeOrNull("WindowManager");
            placeholder?.QueueFree();

            wm = new GodotWindowManager { Name = "WindowManager" };
            AddChild(wm);
        }

        MoveChild(wm, GetChildCount() - 1);
        return wm;
    }

    public override void _ExitTree()
    {
        _app?.Dispose();
        _app = null;
        base._ExitTree();
    }
}
