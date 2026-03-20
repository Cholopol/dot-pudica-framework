using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.ReflectionProxy;

[DotPudicaView(typeof(ReflectionProxySampleViewModel))]
public partial class ReflectionProxySampleView : Control
{
    [BindTo(nameof(ReflectionProxySampleViewModel.SampleText),
        Mode = BindingMode.TwoWay,
        TargetProperty = nameof(ReflectionProbeControl.ValueText),
        SourceEvent = nameof(ReflectionProbeControl.ValueTextChanged))]
    private ReflectionProbeControl _probe = null!;

    [BindTo(nameof(ReflectionProxySampleViewModel.StatusText), Mode = BindingMode.OneWay)]
    private Label _statusLabel = null!;

    private Label _resultLabel = null!;
    private Button _disposeCheckButton = null!;
    private Button _stressTestButton = null!;
    private Label _stressSummaryLabel = null!;
    private bool _bindingDisposed;
    private bool _stressRunning;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(560, 280);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        layout.AddChild(new Label
        {
            Text = "ReflectionProxy sample: first verify TwoWay binding manually, then run the automated 100x stress check."
        });

        _probe = new ReflectionProbeControl();
        layout.AddChild(_probe);

        _statusLabel = new Label();
        layout.AddChild(_statusLabel);

        _disposeCheckButton = new Button
        {
            Text = "Run Dispose Check"
        };
        _disposeCheckButton.Pressed += OnDisposeCheckPressed;
        layout.AddChild(_disposeCheckButton);

        _resultLabel = new Label
        {
            Text = "Result: not run"
        };
        layout.AddChild(_resultLabel);

        _stressTestButton = new Button
        {
            Text = "Run 100x Stress Test"
        };
        _stressTestButton.Pressed += OnStressTestPressed;
        layout.AddChild(_stressTestButton);

        _stressSummaryLabel = new Label
        {
            Text = "Stress: not run"
        };
        layout.AddChild(_stressSummaryLabel);

        ViewModel = new ReflectionProxySampleViewModel();
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        _disposeCheckButton.Pressed -= OnDisposeCheckPressed;
        _stressTestButton.Pressed -= OnStressTestPressed;

        if (!_bindingDisposed)
        {
            DotPudicaDispose();
            _bindingDisposed = true;
        }

        base._ExitTree();
    }

    private void OnDisposeCheckPressed()
    {
        var beforeDispose = ViewModel?.SampleText;

        if (!_bindingDisposed)
        {
            DotPudicaDispose();
            _bindingDisposed = true;
        }

        _probe.ValueText = "Changed after DotPudicaDispose";
        var afterDispose = ViewModel?.SampleText;
        var disconnected = beforeDispose == afterDispose;

        _resultLabel.Text = $"Result: disconnected={disconnected}";
        _disposeCheckButton.Disabled = true;

        GD.Print($"[ReflectionProxySample] Binding disconnected after dispose: {disconnected}. Before='{beforeDispose}', After='{afterDispose}'");
    }

    private async void OnStressTestPressed()
    {
        if (_stressRunning)
            return;

        _stressRunning = true;
        _stressTestButton.Disabled = true;
        _stressSummaryLabel.Text = "Stress: running...";

        try
        {
            var summary = await RunStressTestAsync(iterations: 100);
            _stressSummaryLabel.Text = summary.UiText;
            GD.Print(summary.LogText);
        }
        finally
        {
            _stressRunning = false;
            _stressTestButton.Disabled = false;
        }
    }

    private async Task<StressSummary> RunStressTestAsync(int iterations)
    {
        ForceCollection();
        var start = CaptureMemorySnapshot("start");

        var successCount = 0;
        var failedBindings = 0;
        var failedDisconnects = 0;

        for (var i = 0; i < iterations; i++)
        {
            var caseView = new ReflectionProxyStressCaseView
            {
                Visible = false
            };

            AddChild(caseView);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var bindingWorked = caseView.RunBindingInteraction(i + 1);
            var disconnectWorked = caseView.RunDisposeValidation(i + 1);

            if (bindingWorked && disconnectWorked)
                successCount++;
            else
            {
                if (!bindingWorked)
                    failedBindings++;
                if (!disconnectWorked)
                    failedDisconnects++;
            }

            RemoveChild(caseView);
            caseView.QueueFree();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        ForceCollection();
        var end = CaptureMemorySnapshot("end");

        var managedDelta = end.ManagedBytes - start.ManagedBytes;
        var privateDelta = end.PrivateBytes - start.PrivateBytes;

        return new StressSummary(
            $"Stress: success={successCount}/{iterations}, bindingFail={failedBindings}, disconnectFail={failedDisconnects}, managedDelta={FormatBytes(managedDelta)}, privateDelta={FormatBytes(privateDelta)}",
            $"[ReflectionProxyStress] iterations={iterations}, success={successCount}, bindingFail={failedBindings}, disconnectFail={failedDisconnects}, managedStart={FormatBytes(start.ManagedBytes)}, managedEnd={FormatBytes(end.ManagedBytes)}, managedDelta={FormatBytes(managedDelta)}, privateStart={FormatBytes(start.PrivateBytes)}, privateEnd={FormatBytes(end.PrivateBytes)}, privateDelta={FormatBytes(privateDelta)}");
    }

    private static void ForceCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static MemorySnapshot CaptureMemorySnapshot(string label)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return new MemorySnapshot(label, GC.GetTotalMemory(forceFullCollection: true), process.PrivateMemorySize64);
    }

    private static string FormatBytes(long bytes)
    {
        var abs = Math.Abs(bytes);
        string suffix;
        double value;

        if (abs >= 1024L * 1024L)
        {
            suffix = "MB";
            value = bytes / (1024d * 1024d);
        }
        else if (abs >= 1024L)
        {
            suffix = "KB";
            value = bytes / 1024d;
        }
        else
        {
            suffix = "B";
            value = bytes;
        }

        return $"{value:F2} {suffix}";
    }

    private readonly record struct MemorySnapshot(string Label, long ManagedBytes, long PrivateBytes);
    private readonly record struct StressSummary(string UiText, string LogText);
}

[DotPudicaView(typeof(ReflectionProxySampleViewModel))]
public partial class ReflectionProxyStressCaseView : Control
{
    [BindTo(nameof(ReflectionProxySampleViewModel.SampleText),
        Mode = BindingMode.TwoWay,
        TargetProperty = nameof(ReflectionProbeControl.ValueText),
        SourceEvent = nameof(ReflectionProbeControl.ValueTextChanged))]
    private ReflectionProbeControl _probe = null!;

    private bool _disposed;

    public override void _Ready()
    {
        _probe = new ReflectionProbeControl();
        AddChild(_probe);

        ViewModel = new ReflectionProxySampleViewModel();
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        if (!_disposed)
        {
            DotPudicaDispose();
            _disposed = true;
        }

        base._ExitTree();
    }

    public bool RunBindingInteraction(int iteration)
    {
        var expected = $"Stress update #{iteration}";
        _probe.ValueText = expected;
        return ViewModel?.SampleText == expected;
    }

    public bool RunDisposeValidation(int iteration)
    {
        var beforeDispose = ViewModel?.SampleText;

        if (!_disposed)
        {
            DotPudicaDispose();
            _disposed = true;
        }

        _probe.ValueText = $"Post-dispose update #{iteration}";
        var afterDispose = ViewModel?.SampleText;
        return beforeDispose == afterDispose;
    }
}
