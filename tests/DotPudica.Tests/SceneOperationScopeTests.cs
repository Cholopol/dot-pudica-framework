using DotPudica.Core.Threading;

namespace DotPudica.Tests;

public class SceneOperationScopeTests
{
    [Fact]
    public void Cancel_SignalsToken()
    {
        using var scope = new SceneOperationScope();
        Assert.False(scope.Token.IsCancellationRequested);
        scope.Cancel();
        Assert.True(scope.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateLinkedTokenSource_ChildCancel_DoesNotCancelScene()
    {
        using var scope = new SceneOperationScope();
        using var child = scope.CreateLinkedTokenSource();
        child.Cancel();
        Assert.True(child.IsCancellationRequested);
        Assert.False(scope.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateLinkedTokenSource_SceneCancel_CancelsChild()
    {
        using var scope = new SceneOperationScope();
        using var child = scope.CreateLinkedTokenSource();
        scope.Cancel();
        Assert.True(child.IsCancellationRequested);
    }

    [Fact]
    public async Task CancelledTask_DoesNotWriteViewModel()
    {
        using var scope = new SceneOperationScope();
        var applied = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var work = Task.Run(async () =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), scope.Token);
                Interlocked.Increment(ref applied);
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        });

        await started.Task;
        scope.Cancel();
        await work;

        Assert.Equal(0, applied);
    }

    [Fact]
    public void Dispose_CancelsAndThrowsOnTokenAccess()
    {
        var scope = new SceneOperationScope();
        scope.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = scope.Token);
    }
}
