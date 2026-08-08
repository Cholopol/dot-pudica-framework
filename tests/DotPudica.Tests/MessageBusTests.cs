using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using DotPudica.Core.Messaging;
using DotPudica.Core.Runtime;
using DotPudica.Core.ViewModels;

namespace DotPudica.Tests;

/// <summary>
/// MessageBus / ViewModelBase message contract: delivery, unregistration, and no delivery after weak reference collection.
/// These are hard assertions of framework capabilities; the Showcase Messaging page is for manual demonstration only and cannot replace this file.
/// </summary>
[Collection(FrameworkStaticCollection.Name)]
public sealed class MessageBusTests
{
    public MessageBusTests()
    {
        FrameworkRuntime.Reset();
        MessageBus.Reset();
    }

    [Fact]
    public void MessageBus_Send_DeliversToRegisteredRecipient()
    {
        var counter = new Counter();
        var recipient = new CountingRecipient(counter);
        MessageBus.Register<CountingRecipient, ProbeMessage>(
            recipient,
            static (r, _) => r.Notify());

        MessageBus.Send(new ProbeMessage(1));

        Assert.Equal(1, counter.Value);
        MessageBus.UnregisterAll(recipient);
    }

    [Fact]
    public void MessageBus_Send_DeliversToMultipleRecipients()
    {
        var aCounter = new Counter();
        var bCounter = new Counter();
        var a = new CountingRecipient(aCounter);
        var b = new CountingRecipient(bCounter);
        MessageBus.Register<CountingRecipient, ProbeMessage>(a, static (r, _) => r.Notify());
        MessageBus.Register<CountingRecipient, ProbeMessage>(b, static (r, _) => r.Notify());

        MessageBus.Send(new ProbeMessage(1));

        Assert.Equal(1, aCounter.Value);
        Assert.Equal(1, bCounter.Value);
        MessageBus.UnregisterAll(a);
        MessageBus.UnregisterAll(b);
    }

    [Fact]
    public void MessageBus_UnregisterAll_StopsDelivery()
    {
        var counter = new Counter();
        var recipient = new CountingRecipient(counter);
        MessageBus.Register<CountingRecipient, ProbeMessage>(
            recipient,
            static (r, _) => r.Notify());
        MessageBus.UnregisterAll(recipient);

        MessageBus.Send(new ProbeMessage(1));

        Assert.Equal(0, counter.Value);
    }

    [Fact]
    public void MessageBus_DuplicateRegister_SameRecipientAndMessage_Throws()
    {
        var recipient = new object();
        MessageBus.Register<object, ProbeMessage>(recipient, static (_, _) => { });

        Assert.Throws<InvalidOperationException>(() =>
            MessageBus.Register<object, ProbeMessage>(recipient, static (_, _) => { }));

        MessageBus.UnregisterAll(recipient);
    }

    [Fact]
    public void WeakSubscription_StopsAfterRecipientIsCollected()
    {
        var aliveCounter = new Counter();
        var deadCounter = new Counter();

        var alive = RegisterCountingRecipient(aliveCounter);
        var weakDoomed = RegisterDoomedRecipient(deadCounter);

        ForceCollect();

        Assert.False(weakDoomed.IsAlive,
            "The probe object should be collectable by GC; if it fails, the test still holds an unexpected strong reference, and the weak subscription contract cannot be satisfied.");

        MessageBus.Send(new ProbeMessage(1));

        Assert.Equal(1, aliveCounter.Value);
        Assert.Equal(0, deadCounter.Value);

        MessageBus.UnregisterAll(alive);
    }

    [Fact]
    public void ViewModelBase_Dispose_UnregistersMessengerHandlers()
    {
        var counter = new Counter();
        var vm = new CountingViewModel(counter);

        MessageBus.Send(new ProbeMessage(1));
        Assert.Equal(1, counter.Value);

        vm.Dispose();
        MessageBus.Send(new ProbeMessage(2));
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public void ViewModelBase_Register_And_MessageBus_Register_OnSameRecipient_Throws()
    {
        // The Showcase Messaging page must therefore split into two recipients; this locks in that constraint.
        var vm = new EmptyViewModel();
        MessageBus.Register<EmptyViewModel, ProbeMessage>(vm, static (_, _) => { });

        Assert.Throws<InvalidOperationException>(() =>
            vm.RegisterPublic<ProbeMessage>((_, _) => { }));

        vm.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CountingRecipient RegisterCountingRecipient(Counter counter)
    {
        var recipient = new CountingRecipient(counter);
        MessageBus.Register<CountingRecipient, ProbeMessage>(
            recipient,
            static (r, _) => r.Notify());
        return recipient;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterDoomedRecipient(Counter counter)
    {
        var doomed = new CountingRecipient(counter);
        MessageBus.Register<CountingRecipient, ProbeMessage>(
            doomed,
            static (r, _) => r.Notify());
        var weak = new WeakReference(doomed);
        doomed = null!;
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ForceCollect()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class Counter
    {
        public int Value;
        public void Increment() => Interlocked.Increment(ref Value);
    }

    private sealed class CountingRecipient(Counter counter)
    {
        public void Notify() => counter.Increment();
    }

    private sealed class CountingViewModel : ViewModelBase
    {
        public CountingViewModel(Counter counter)
        {
            Register<ProbeMessage>((_, _) => counter.Increment());
        }
    }

    private sealed class EmptyViewModel : ViewModelBase
    {
        public void RegisterPublic<TMessage>(MessageHandler<ViewModelBase, TMessage> handler)
            where TMessage : class
            => Register(handler);
    }

    private sealed record ProbeMessage(int Id);
}
