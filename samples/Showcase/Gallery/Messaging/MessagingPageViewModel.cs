using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Messaging;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Messaging;

/// <summary>
/// Messaging Gallery: page-held subscriptions vs weak-reference GC probe.
/// MessageBus and ViewModelBase.Register use separate recipient instances.
/// </summary>
public partial class MessagingPageViewModel : ViewModelBase
{
    private readonly ViewModelPathRecipient _viewModelPathRecipient;
    private WeakReference<GcProbeSubscriber>? _weakSubscriber;
    private int _busReceivedCount;
    private int _vmPathReceivedCount;
    private int _weakReceivedSnapshot;
    private int _sequence;
    private bool _weakCollected;
    private bool _gcAttemptFailed;

    public MessagingPageViewModel()
    {
        MessageBus.Register<MessagingPageViewModel, PingMessage>(this, static (vm, _) =>
        {
            vm._busReceivedCount++;
            vm.RefreshLabels();
        });

        _viewModelPathRecipient = new ViewModelPathRecipient(OnViewModelPathMessage);
        RefreshLabels();
    }

    [ObservableProperty]
    private string _guideText = "";

    [ObservableProperty]
    private string _ownedSectionTitle = "";

    [ObservableProperty]
    private string _busRegisterText = "";

    [ObservableProperty]
    private string _viewModelRegisterText = "";

    [ObservableProperty]
    private string _ownedFooterText = "";

    [ObservableProperty]
    private string _weakSectionTitle = "";

    [ObservableProperty]
    private string _subscriberStatusText = "";

    [ObservableProperty]
    private string _lastSendText = "—";

    [RelayCommand]
    private void CreateSubscriber()
    {
        _weakSubscriber = new WeakReference<GcProbeSubscriber>(new GcProbeSubscriber());
        _weakReceivedSnapshot = 0;
        _weakCollected = false;
        _gcAttemptFailed = false;
        RefreshLabels();
    }

    [RelayCommand]
    private void SendMessage()
    {
        _sequence++;
        var message = new PingMessage($"ping-{_sequence}", _sequence);
        LastSendText = $"#{message.Sequence} {message.Text}";
        MessageBus.Send(message);
        RefreshLabels();
    }

    [RelayCommand]
    private void ForceGc()
    {
        CaptureWeakReceivedSnapshot();

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var stillAlive = _weakSubscriber is not null && IsWeakSubscriberAlive();
        _weakCollected = _weakSubscriber is not null && !stillAlive;
        _gcAttemptFailed = _weakSubscriber is not null && stillAlive;
        RefreshLabels();
    }

    protected override void OnDispose()
    {
        _viewModelPathRecipient.Dispose();
        base.OnDispose();
    }

    private void OnViewModelPathMessage(PingMessage message)
    {
        _vmPathReceivedCount++;
        RefreshLabels();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void CaptureWeakReceivedSnapshot()
    {
        if (_weakSubscriber?.TryGetTarget(out var live) == true)
            _weakReceivedSnapshot = live.ReceivedCount;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsWeakSubscriberAlive()
        => _weakSubscriber?.TryGetTarget(out _) == true;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int? TryReadLiveWeakCount()
    {
        if (_weakSubscriber?.TryGetTarget(out var live) == true)
            return live.ReceivedCount;
        return null;
    }

    private void RefreshLabels()
    {
        GuideText = "";

        OwnedSectionTitle = "Page-held · survives GC";
        BusRegisterText = $"Bus: {_busReceivedCount}";
        ViewModelRegisterText = $"VM path: {_vmPathReceivedCount}";
        OwnedFooterText = _weakCollected
            ? "Weak probe gone — owned still counts up."
            : _gcAttemptFailed
                ? "GC missed weak probe — retry Force GC."
                : "Strong refs keep both paths alive.";

        WeakSectionTitle = "Weak probe · GC target";

        if (_weakSubscriber is null)
        {
            SubscriberStatusText = "Not created";
        }
        else
        {
            var liveCount = TryReadLiveWeakCount();
            if (liveCount is { } count)
            {
                _weakReceivedSnapshot = count;
                _weakCollected = false;
                SubscriberStatusText = _gcAttemptFailed
                    ? $"GC failed · {count} msgs"
                    : $"Alive · {count} msgs";
            }
            else
            {
                _weakCollected = true;
                _gcAttemptFailed = false;
                SubscriberStatusText = $"Collected · was {_weakReceivedSnapshot} msgs";
            }
        }
    }

    private sealed class ViewModelPathRecipient : ViewModelBase
    {
        public ViewModelPathRecipient(Action<PingMessage> onMessage)
        {
            Register<PingMessage>((_, message) => onMessage(message));
        }
    }
}
