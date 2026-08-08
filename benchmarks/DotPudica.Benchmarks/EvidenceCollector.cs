using System.Diagnostics;
using System.Text.Json;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

public static class EvidenceCollector
{
    public static void Write(string outputPath)
    {
        var coalesce = new List<object>();
        foreach (var n in new[] { 1_000, 10_000 })
        {
            coalesce.Add(MeasureUiThreadBurst(n));
            coalesce.Add(MeasureBackgroundCoalesced(n));
        }

        var setup = new List<object>();
        foreach (var bindCount in new[] { 10, 50, 100 })
        {
            setup.Add(MeasureSetup(bindCount, typed: true));
            setup.Add(MeasureSetup(bindCount, typed: false));
        }

        var payload = new
        {
            kind = "core-evidence",
            capturedAtUtc = DateTime.UtcNow.ToString("O"),
            coalesce,
            setup,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }

    private static object MeasureSetup(int bindCount, bool typed)
    {
        var vm = new IntViewModel { Value = 1 };
        var sw = Stopwatch.StartNew();
        if (typed)
        {
            var bindings = new PropertyBinding<int, int>[bindCount];
            for (var i = 0; i < bindCount; i++)
            {
                bindings[i] = new PropertyBinding<int, int>(
                    new ZeroAllocIntProxy(),
                    new TypedBindingPath<IntViewModel, int>(
                        static x => x.Value,
                        static (x, v) => x.Value = v,
                        ["Value"]),
                    BindingMode.OneWay);
                bindings[i].Bind(vm);
            }
            sw.Stop();
            var bindMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            for (var i = 0; i < bindCount; i++)
                bindings[i].Dispose();
            sw.Stop();
            return new
            {
                mode = "typed-setup",
                bindCount,
                bindMs,
                disposeMs = sw.Elapsed.TotalMilliseconds,
            };
        }

        {
            var bindings = new PropertyBinding[bindCount];
            for (var i = 0; i < bindCount; i++)
            {
                bindings[i] = new PropertyBinding(
                    new ObjectTargetProxy(),
                    new TypedBindingPath<IntViewModel, int>(
                        static x => x.Value,
                        static (x, v) => x.Value = v,
                        ["Value"]),
                    BindingMode.OneWay);
                bindings[i].Bind(vm);
            }
            sw.Stop();
            var bindMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            for (var i = 0; i < bindCount; i++)
                bindings[i].Dispose();
            sw.Stop();
            return new
            {
                mode = "object-setup",
                bindCount,
                bindMs,
                disposeMs = sw.Elapsed.TotalMilliseconds,
            };
        }
    }

    private static object MeasureUiThreadBurst(int n)
    {
        var vm = new StringViewModel { Name = "initial" };
        var proxy = new CountingStringProxy();
        using var binding = new PropertyBinding<string, string>(
            proxy,
            new TypedBindingPath<StringViewModel, string>(
                static x => x.Name,
                static (x, v) => x.Name = v,
                ["Name"]),
            BindingMode.OneWay);
        binding.Bind(vm);

        var before = proxy.SetValueCallCount;
        for (var i = 0; i < n; i++)
            vm.Name = $"value-{i}";

        return new
        {
            mode = "ui-thread-burst",
            sourceUpdates = n,
            pendingPosts = 0,
            targetWrites = proxy.SetValueCallCount - before,
            finalValue = proxy.Value,
        };
    }

    private static object MeasureBackgroundCoalesced(int n)
    {
        var dispatcher = new QueuedUiDispatcher { HasAccess = true };
        var vm = new StringViewModel { Name = "initial" };
        var proxy = new CountingStringProxy();
        using var binding = new PropertyBinding<string, string>(
            proxy,
            new TypedBindingPath<StringViewModel, string>(
                static x => x.Name,
                static (x, v) => x.Name = v,
                ["Name"]),
            BindingMode.OneWay,
            dispatcher: dispatcher);
        binding.Bind(vm);
        dispatcher.RunAll();

        dispatcher.HasAccess = false;
        var before = proxy.SetValueCallCount;
        for (var i = 0; i < n; i++)
            vm.Name = $"value-{i}";
        var pending = dispatcher.PendingCount;
        dispatcher.HasAccess = true;
        dispatcher.RunAll();

        return new
        {
            mode = "background-coalesced",
            sourceUpdates = n,
            pendingPosts = pending,
            targetWrites = proxy.SetValueCallCount - before,
            finalValue = proxy.Value,
        };
    }
}
