using DotPudica.Core.Interactivity;

namespace DotPudica.Tests;

public class InteractionRequestTests
{
    [Fact]
    public void Raise_WithSubscriber_InvokesHandler()
    {
        var request = new InteractionRequest();
        var invoked = false;
        request.Raised += (sender, args) =>
        {
            invoked = true;
            Assert.Same(request, sender);
            Assert.Same(EventArgs.Empty, args);
        };

        request.Raise();

        Assert.True(invoked);
    }

    [Fact]
    public void Raise_WithoutSubscriber_DoesNotThrow()
    {
        var request = new InteractionRequest();

        var exception = Record.Exception(() => request.Raise());

        Assert.Null(exception);
    }

    [Fact]
    public void Raise_AfterUnsubscribe_DoesNotInvokeHandler()
    {
        var request = new InteractionRequest();
        var invokeCount = 0;
        void Handler(object? sender, EventArgs args) => invokeCount++;

        request.Raised += Handler;
        request.Raise();
        request.Raised -= Handler;
        request.Raise();

        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void Raise_WithMultipleSubscribers_InvokesAll()
    {
        var request = new InteractionRequest();
        var count = 0;

        request.Raised += (_, _) => count++;
        request.Raised += (_, _) => count++;
        request.Raise();

        Assert.Equal(2, count);
    }

    [Fact]
    public void RaiseT_PassesStronglyTypedContext()
    {
        var request = new InteractionRequest<string>();
        string? received = null;

        request.Raised += (_, args) =>
        {
            received = args.Context;
            Assert.Null(args.Callback);
        };

        request.Raise("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void RaiseT_WithCallback_InvokesVmCallbackWhenViewCompletes()
    {
        var request = new InteractionRequest<int>();
        var callbackContext = 0;
        var callbackInvoked = false;

        request.Raised += (_, args) =>
        {
            Assert.Equal(42, args.Context);
            Assert.NotNull(args.Callback);
            args.Callback!();
        };

        request.Raise(42, ctx =>
        {
            callbackInvoked = true;
            callbackContext = ctx;
        });

        Assert.True(callbackInvoked);
        Assert.Equal(42, callbackContext);
    }

    [Fact]
    public void RaiseT_WithCallback_DoesNotInvokeWhenViewSkipsCallback()
    {
        var request = new InteractionRequest<string>();
        var callbackInvoked = false;

        request.Raised += (_, _) => { /* View did not invoke Callback */ };
        request.Raise("x", _ => callbackInvoked = true);

        Assert.False(callbackInvoked);
    }

    [Fact]
    public void RaiseT_WithoutSubscriber_DoesNotThrow()
    {
        var request = new InteractionRequest<object>();

        var exception = Record.Exception(() => request.Raise(new object(), _ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void RaiseT_AfterUnsubscribe_DoesNotInvokeHandler()
    {
        var request = new InteractionRequest<int>();
        var invokeCount = 0;
        void Handler(object? sender, InteractionEventArgs<int> args) => invokeCount++;

        request.Raised += Handler;
        request.Raise(1);
        request.Raised -= Handler;
        request.Raise(2);

        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void RaiseT_WithMultipleSubscribers_InvokesAll()
    {
        var request = new InteractionRequest<string>();
        var count = 0;

        request.Raised += (_, _) => count++;
        request.Raised += (_, _) => count++;
        request.Raise("a");

        Assert.Equal(2, count);
    }
}
