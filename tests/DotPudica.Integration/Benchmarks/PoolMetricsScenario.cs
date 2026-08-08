using System.Diagnostics;
using DotPudica.Godot.ObjectPool;
using DotPudica.Godot.Views;
using DotPudica.Integration.Fixtures;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Benchmarks;

public sealed class PoolMetricsScenario : IBenchmarkScenario
{
    public string Name => "PoolMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        metrics.AddPool(await MeasureViewPoolAsync(host, iterations: 50));
        metrics.AddPool(await MeasureWindowPoolAsync(host, iterations: 50));
    }

    private static async Task<PoolMetric> MeasureViewPoolAsync(Node host, int iterations)
    {
        var pool = NodePool.Create<PooledItemView>(maxSize: 4);
        var created = 0;
        var reused = 0;
        PooledItemView? last = null;
        var sw = Stopwatch.StartNew();

        try
        {
            for (var i = 0; i < iterations; i++)
            {
                var view = pool.Allocate();
                if (last is null)
                    created++;
                else if (ReferenceEquals(view, last))
                    reused++;
                else
                    created++;

                host.AddChild(view);
                var vm = new PooledItemViewModel { Title = $"n{i}" };
                view.BindShared(vm);
                await IntegrationTestHelpers.WaitProcessFrame(host);

                view.GetParent()?.RemoveChild(view);
                pool.Free(view);
                last = view;
                vm.Dispose();
                await IntegrationTestHelpers.WaitProcessFrame(host);
            }
        }
        finally
        {
            sw.Stop();
            if (last is not null && GodotObject.IsInstanceValid(last) && last.GetParent() is null)
                last.QueueFree();
            pool.Dispose();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }

        return new PoolMetric
        {
            Mode = "view-pool",
            Iterations = iterations,
            CreatedNodes = created,
            ReusedCount = reused,
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
        };
    }

    private static async Task<PoolMetric> MeasureWindowPoolAsync(Node host, int iterations)
    {
        var wm = new GodotWindowManager { Name = "BenchWindowManager_Pool" };
        host.AddChild(wm);

        AppContext? app = null;
        try
        {
            try
            {
                _ = AppContext.Current;
            }
            catch (InvalidOperationException)
            {
                app = new AppContext().Initialize(null, wm);
            }

            var manager = app?.WindowManager ?? wm;
            manager.ConfigurePool<PooledWindow>(maxSize: 4);

            var created = 0;
            var reused = 0;
            PooledWindow? last = null;
            var sw = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var window = manager.ShowPooled<PooledWindow>();
                await IntegrationTestHelpers.WaitFrames(host, 1);

                if (last is null)
                    created++;
                else if (ReferenceEquals(window, last))
                    reused++;
                else
                    created++;

                await manager.Dismiss(window, ignoreAnimation: true).WaitForFinish();
                await IntegrationTestHelpers.WaitProcessFrame(host);
                last = window;
            }

            sw.Stop();
            return new PoolMetric
            {
                Mode = "window-pool",
                Iterations = iterations,
                CreatedNodes = created,
                ReusedCount = reused,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
            };
        }
        finally
        {
            app?.Dispose();
            wm.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }
}
