using DotPudica.Core.ViewModels;
using System.Threading;
using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Probes;
using Samples.Showcase.Shared.Services;
using AppContext = DotPudica.Godot.AppContext;
using Environment = System.Environment;

namespace Samples.Showcase.Gallery.ThreadingLab;

/// <summary>
/// Threading lab — seven probes (A–G) exercise dispatcher and binding thread contracts.
/// Verdict logic lives in <c>Shared/Probes</c>; this page drives and displays results only.
/// Intentionally not MVVM-bound — the page tests the binding pipeline itself.
/// </summary>
public partial class ThreadingLabPage : ShowcasePageWindow
{
    private IUiDispatcher _dispatcher = UiDispatcher.Immediate;
    private long _frameCounter;

    private readonly ThreadAffinityProbe _probeA = new();
    private readonly CoalesceProbe _probeB = new();
    private readonly StaleWorkProbe _probeC = new();
    private readonly DispatchOrderProbe _probeD = new();
    private readonly BackpressureProbe _probeE = new();
    private readonly CancelLifecycleProbe _probeF = new();
    private readonly ContractViolationProbe _probeG = new();

    private ProbeCard _cardA = null!;
    private ProbeCard _cardB = null!;
    private ProbeCard _cardC = null!;
    private ProbeCard _cardD = null!;
    private ProbeCard _cardE = null!;
    private ProbeCard _cardF = null!;
    private ProbeCard _cardG = null!;

    public override void _Ready()
    {
        _dispatcher = UiDispatcher.FromSynchronizationContext(
            Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext"));

        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Seven probes for dispatcher and binding thread contracts.");

        _cardA = AddCard(root, "A", "Thread affinity", "Non-UI writes == 0; at least one write", OnRunA);
        _cardB = AddCard(root, "B", "Coalesce final state", "Write count in [1,2]; final value correct", OnRunB);
        _cardC = AddCard(root, "C", "Stale delivery drop", "Final value from new VM; no stale prefix after rebind", OnRunC);
        _cardD = AddCard(root, "D", "Dispatch order", "Strict FIFO sequence; all executed", OnRunD);
        _cardE = AddCard(root, "E", "Backpressure evidence", "Evidence table only — no FAIL verdict", OnRunE);
        _cardF = AddCard(root, "F", "Cancel lifecycle", "Enter/exit balanced; zero writes after exit; no exceptions; CTS == 0", OnRunF);
        _cardG = AddCard(root, "G", "Contract violations", "TwoWay/lifecycle throw off UI thread; collection gap documented", OnRunG);
    }

    public override void _Process(double delta)
    {
        Interlocked.Increment(ref _frameCounter);
    }

    private static ProbeCard AddCard(
        Container parent, string id, string name, string expectation, Action onRun)
    {
        var card = new ProbeCard(id, name, expectation);
        card.RunRequested += onRun;
        parent.AddChild(card);
        return card;
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }


    private async void OnRunA()
    {
        _cardA.SetRunning();
        try
        {
            var mainThreadId = Environment.CurrentManagedThreadId;
            _probeA.Reset(mainThreadId);

            using var runtime = new DotPudicaViewRuntime<ProbeTitleViewModel>();
            runtime.CaptureUiContext();
            var vm = new ProbeTitleViewModel { Title = "before" };
            runtime.SetViewModel(vm, ViewModelOwnership.Owned);

            var path = new TypedBindingPath<ProbeTitleViewModel, string>(
                static x => x.Title, static (x, v) => x.Title = v, ["Title"]);
            var proxy = new RecordingStringProxy(_ => _probeA.RecordWrite());
            runtime.BindProperty(proxy, path, BindingMode.OneWay);

            await WaitFrames(1);
            await Task.Run(() => vm.Title = "from-worker");
            await WaitFrames(3);

            _cardA.ApplyResult(_probeA.Evaluate());
        }
        catch (Exception ex)
        {
            _cardA.ApplyError(ex.Message);
        }
    }


    private async void OnRunB()
    {
        _cardB.SetRunning();
        try
        {
            const int writes = 2000;
            _probeB.Reset($"v{writes - 1}");

            using var runtime = new DotPudicaViewRuntime<ProbeTitleViewModel>();
            runtime.CaptureUiContext();
            var vm = new ProbeTitleViewModel { Title = "initial" };
            vm.PropertyChanged += (_, _) => _probeB.OnPropertyChanged();
            runtime.SetViewModel(vm, ViewModelOwnership.Owned);

            var path = new TypedBindingPath<ProbeTitleViewModel, string>(
                static x => x.Title, static (x, v) => x.Title = v, ["Title"]);
            var proxy = new RecordingStringProxy(_probeB.OnTargetWrite);
            runtime.BindProperty(proxy, path, BindingMode.OneWay);

            await WaitFrames(1);
            await Task.Run(() =>
            {
                for (var i = 0; i < writes; i++)
                    vm.Title = $"v{i}";
            });
            await WaitFrames(3);

            _cardB.ApplyResult(_probeB.Evaluate());
        }
        catch (Exception ex)
        {
            _cardB.ApplyError(ex.Message);
        }
    }


    private async void OnRunC()
    {
        _cardC.SetRunning();
        ProbeTitleViewModel? first = null;
        ProbeTitleViewModel? second = null;
        try
        {
            _probeC.Reset("second", "stale-");

            using var runtime = new DotPudicaViewRuntime<ProbeTitleViewModel>();
            runtime.CaptureUiContext();
            first = new ProbeTitleViewModel { Title = "first" };
            second = new ProbeTitleViewModel { Title = "second" };
            runtime.SetViewModel(first, ViewModelOwnership.External);

            var path = new TypedBindingPath<ProbeTitleViewModel, string>(
                static x => x.Title, static (x, v) => x.Title = v, ["Title"]);
            var proxy = new RecordingStringProxy(_probeC.RecordWrite);
            runtime.BindProperty(proxy, path, BindingMode.OneWay);

            var flood = Task.Run(() =>
            {
                for (var i = 0; i < 64; i++)
                    first.Title = $"stale-{i}";
            });
            runtime.SetViewModel(second, ViewModelOwnership.External);
            await flood;
            await WaitFrames(3);

            _cardC.ApplyResult(_probeC.Evaluate());
        }
        catch (Exception ex)
        {
            _cardC.ApplyError(ex.Message);
        }
        finally
        {
            first?.Dispose();
            second?.Dispose();
        }
    }


    private async void OnRunD()
    {
        _cardD.SetRunning();
        try
        {
            _probeD.Reset();
            const int n = 300;

            await Task.Run(() =>
            {
                for (var i = 1; i <= n; i++)
                {
                    var seq = i;
                    var postFrame = Interlocked.Read(ref _frameCounter);
                    _probeD.RecordPosted();
                    _dispatcher.Post(() =>
                    {
                        var execFrame = Interlocked.Read(ref _frameCounter);
                        _probeD.RecordExecuted(seq, postFrame, execFrame);
                    });
                }
            });
            await WaitFrames(10);

            _cardD.ApplyResult(_probeD.Evaluate());
        }
        catch (Exception ex)
        {
            _cardD.ApplyError(ex.Message);
        }
    }

 
    private async void OnRunE()
    {
        _cardE.SetRunning();
        try
        {
            const int total = 10_000;

            // Direct Post storm: 10000 items enqueued one by one, observe per-frame execution count and frames to completion.
            _probeE.Reset("post-storm(10000 Posts)", total);
            var remaining = total;
            var stormDone = new TaskCompletionSource();
            await Task.Run(() =>
            {
                for (var i = 0; i < total; i++)
                {
                    _dispatcher.Post(() =>
                    {
                        _probeE.RecordExecuted(Interlocked.Read(ref _frameCounter));
                        if (Interlocked.Decrement(ref remaining) == 0)
                            stormDone.TrySetResult();
                    });
                }
            });
            await stormDone.Task;
            await WaitFrames(3);
            var stormResult = _probeE.Evaluate();

            // LatestSnapshotMailbox: background keeps only the latest snapshot, UI drains once per frame.
            var mailbox = new LatestSnapshotMailbox<int>();
            _probeE.Reset("mailbox-drain(10000 published)", total);
            var flood = Task.Run(() =>
            {
                for (var i = 1; i <= total; i++)
                    mailbox.Publish(i);
            });

            var drained = 0;
            var guard = 0;
            while ((!flood.IsCompleted || drained == 0) && guard++ < 3000)
            {
                await WaitFrames(1);
                if (mailbox.TryDrainLatest(out _))
                {
                    drained++;
                    _probeE.RecordExecuted(Interlocked.Read(ref _frameCounter));
                }
            }
            await WaitFrames(3);
            if (mailbox.TryDrainLatest(out _))
            {
                drained++;
                _probeE.RecordExecuted(Interlocked.Read(ref _frameCounter));
            }
            var mailboxResult = _probeE.Evaluate();

            var combined = $"Post storm: {stormResult.Observed}\nMailbox drain: {mailboxResult.Observed}";
            _cardE.ApplyResult(mailboxResult with { Observed = combined });
        }
        catch (Exception ex)
        {
            _cardE.ApplyError(ex.Message);
        }
    }


    private async void OnRunF()
    {
        _cardF.SetRunning();
        try
        {
            _probeF.Reset();
            var matchService = AppContext.Current.Services.GetRequiredService<IShowcaseMatchService>();
            const int iterations = 20;
            var activeCts = 0;
            var tasks = new List<Task>(iterations);

            for (var i = 0; i < iterations; i++)
            {
                using var scope = new SceneOperationScope();
                _probeF.OnEnter();
                var cts = scope.CreateLinkedTokenSource();
                Interlocked.Increment(ref activeCts);

                tasks.Add(RunOnceAsync(matchService, cts, () => Interlocked.Decrement(ref activeCts)));

                // Simulate immediate scene exit churn: cancel and dispose scope before the current match completes.
                scope.Cancel();
                _probeF.OnExit();
            }

            await Task.WhenAll(tasks);
            await WaitFrames(2);

            _probeF.ActiveCtsAtEnd = Volatile.Read(ref activeCts);
            _cardF.ApplyResult(_probeF.Evaluate());
        }
        catch (Exception ex)
        {
            _cardF.ApplyError(ex.Message);
        }
    }

    private async Task RunOnceAsync(IShowcaseMatchService service, CancellationTokenSource cts, Action onDone)
    {
        try
        {
            await service.MatchRoomAsync(cts.Token).ConfigureAwait(false);
            // Completed without cancellation: indicates writes still happen after exit, violating Probe F's expectation.
            _dispatcher.Post(_probeF.OnResultAfterExit);
        }
        catch (OperationCanceledException)
        {
            // Expected path: scene exit cancellation, operation terminates normally.
        }
        catch (Exception)
        {
            _dispatcher.Post(_probeF.OnException);
        }
        finally
        {
            cts.Dispose();
            onDone();
        }
    }


    private async void OnRunG()
    {
        _cardG.SetRunning();
        try
        {
            _probeG.Reset();

            using var runtime = new DotPudicaViewRuntime<ProbeTitleViewModel>();
            runtime.CaptureUiContext();
            var vm = new ProbeTitleViewModel { Title = "vm-value" };
            runtime.SetViewModel(vm, ViewModelOwnership.Owned);

            var path = new TypedBindingPath<ProbeTitleViewModel, string>(
                static x => x.Title, static (x, v) => x.Title = v, ["Title"]);
            var proxy = new RecordingStringProxy();
            runtime.BindProperty(proxy, path, BindingMode.TwoWay);
            await WaitFrames(1);

            // TwoWay target-side change signal from a background thread: expected to throw InvalidOperationException from the binding base class.
            Exception? twoWayEx = null;
            await Task.Run(() =>
            {
                try { proxy.RaiseValueChanged(); }
                catch (Exception ex) { twoWayEx = ex; }
            });
            _probeG.RecordTwoWay(twoWayEx);

            // Lifecycle operation (SetViewModel) called on a background thread: also expected to throw.
            Exception? lifecycleEx = null;
            await Task.Run(() =>
            {
                try { runtime.SetViewModel(new ProbeTitleViewModel { Title = "other" }, ViewModelOwnership.Owned); }
                catch (Exception ex) { lifecycleEx = ex; }
            });
            _probeG.RecordLifecycle(lifecycleEx);

            // Background thread directly mutates the shared ObservableCollection: the framework has no thread guard, recorded as a known gap.
            var inventory = AppContext.Current.Services.GetRequiredService<IInventoryService>();
            var before = inventory.Items.Count;
            Exception? collectionEx = null;
            await Task.Run(() =>
            {
                try
                {
                    inventory.Items.Add(new LoadoutItem(
                        $"probe-g-{Guid.NewGuid():N}",
                        "Probe G item",
                        "Test",
                        Power: 1,
                        EquipSlot: null,
                        Attack: 0,
                        Defense: 0,
                        MaxHpBonus: 0,
                        EnergyBonus: 0));
                }
                catch (Exception ex) { collectionEx = ex; }
            });
            var after = inventory.Items.Count;
            _probeG.RecordCollectionMutation(
                collectionEx is null
                    ? $"Background Add did not throw (count {before}→{after}) — known gap, no guard"
                    : $"Background Add threw {collectionEx.GetType().Name} (unexpected)");

            await WaitFrames(1);
            _cardG.ApplyResult(_probeG.Evaluate());
        }
        catch (Exception ex)
        {
            _cardG.ApplyError(ex.Message);
        }
    }
}
