using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Services;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Tests;

public class SceneScopeTests
{
    [Fact]
    public void Create_ResolvesScopedServicesPerScope()
    {
        var root = new ServiceCollection()
            .AddScoped<ProbeService>()
            .AddTransient<ProbeViewModel>()
            .BuildServiceProvider();

        using var scope1 = SceneScope.Create(root);
        using var scope2 = SceneScope.Create(root);

        var a = scope1.Services.GetRequiredService<ProbeService>();
        var b = scope1.Services.GetRequiredService<ProbeService>();
        var c = scope2.Services.GetRequiredService<ProbeService>();

        Assert.Same(a, b);
        Assert.NotSame(a, c);
    }

    [Fact]
    public void ViewModelFactory_CreatesInjectedViewModel()
    {
        var root = new ServiceCollection()
            .AddSingleton<IMatchClock, FixedMatchClock>()
            .AddTransient<InjectedMatchViewModel>()
            .BuildServiceProvider();

        using var scope = SceneScope.Create(root);
        var vm = scope.ViewModels.Create<InjectedMatchViewModel>();
        Assert.Equal("tick", vm.Label);
    }

    private sealed class ProbeService;

    private sealed class ProbeViewModel : ObservableObject;

    private interface IMatchClock
    {
        string Tick { get; }
    }

    private sealed class FixedMatchClock : IMatchClock
    {
        public string Tick => "tick";
    }

    private sealed class InjectedMatchViewModel : ViewModelBase
    {
        public InjectedMatchViewModel(IMatchClock clock) => Label = clock.Tick;

        public string Label { get; }
    }
}

public class MatchViewModelCancelTests
{
    [Fact]
    public async Task CancelledMatch_DoesNotApplyResult()
    {
        using var scope = new SceneOperationScope();
        var service = new SlowMatchService();
        var vm = new CancelProbeViewModel(service, scope);

        var matchTask = vm.MatchAsync();
        await service.Started.Task;
        scope.Cancel();
        await matchTask;

        Assert.Equal(0, vm.AppliedResultCount);
        Assert.Equal(AsyncOperationState.Cancelled, vm.State);
    }

    private sealed class SlowMatchService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<string> MatchAsync(CancellationToken token)
        {
            Started.SetResult();
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return "room";
        }
    }

    private sealed class CancelProbeViewModel
    {
        private readonly SlowMatchService _service;
        private readonly SceneOperationScope _scope;

        public CancelProbeViewModel(SlowMatchService service, SceneOperationScope scope)
        {
            _service = service;
            _scope = scope;
        }

        public int AppliedResultCount { get; private set; }
        public AsyncOperationState State { get; private set; } = AsyncOperationState.Idle;

        public async Task MatchAsync()
        {
            using var linked = _scope.CreateLinkedTokenSource();
            State = AsyncOperationState.Running;
            try
            {
                var room = await _service.MatchAsync(linked.Token);
                AppliedResultCount++;
                State = AsyncOperationState.Succeeded;
                _ = room;
            }
            catch (OperationCanceledException)
            {
                State = AsyncOperationState.Cancelled;
            }
        }
    }
}
