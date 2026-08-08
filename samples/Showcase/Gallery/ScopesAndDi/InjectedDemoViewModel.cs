using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.Gallery.ScopesAndDi;

/// <summary>
/// Page-specific Transient ViewModel: constructor dependencies are injected via DI, created/destroyed with the scene Scope.
/// Must be registered in ShowcaseBootstrap with <c>services.AddTransient&lt;InjectedDemoViewModel&gt;()</c>,
/// otherwise <c>IViewModelFactory.Create&lt;InjectedDemoViewModel&gt;()</c> will throw a resolution exception.
/// </summary>
public partial class InjectedDemoViewModel : ViewModelBase
{
    private static int _instanceSequence;

    public InjectedDemoViewModel(IProfileService profileService)
    {
        ProfileService = profileService;
        InstanceId = Interlocked.Increment(ref _instanceSequence);
    }

    /// <summary>Shared singleton service injected by DI, proving that root-container registrations are resolvable within a Scope.</summary>
    public IProfileService ProfileService { get; }

    /// <summary>Auto-incremented each time an instance is created from a Scope, used to visually distinguish different Scope instances on the UI.</summary>
    public int InstanceId { get; }

    [ObservableProperty]
    private string _greeting = "";

    protected override void OnDispose()
    {
        Greeting = "(scope disposed)";
        base.OnDispose();
    }
}
