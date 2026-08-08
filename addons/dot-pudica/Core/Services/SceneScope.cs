using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Core.Services;

public interface IViewModelFactory
{
    TViewModel Create<TViewModel>() where TViewModel : ObservableObject;
}

/// <summary>
/// Scene-level DI scope: create when the scene root enters the tree;
/// dispose after View/Window release on leave (owns scoped services).
/// </summary>
public interface ISceneScope : IAsyncDisposable, IDisposable
{
    IServiceProvider Services { get; }

    IViewModelFactory ViewModels { get; }
}

public sealed class SceneScope : ISceneScope
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    private SceneScope(IServiceScope scope)
    {
        _scope = scope;
        ViewModels = new ServiceViewModelFactory(scope.ServiceProvider);
    }

    public IServiceProvider Services => _scope.ServiceProvider;

    public IViewModelFactory ViewModels { get; }

    public static ISceneScope Create(IServiceProvider rootProvider)
    {
        ArgumentNullException.ThrowIfNull(rootProvider);
        var scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();
        return new SceneScope(scopeFactory.CreateScope());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _scope.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class ServiceViewModelFactory(IServiceProvider services) : IViewModelFactory
{
    public TViewModel Create<TViewModel>() where TViewModel : ObservableObject
        => services.GetRequiredService<TViewModel>();
}
