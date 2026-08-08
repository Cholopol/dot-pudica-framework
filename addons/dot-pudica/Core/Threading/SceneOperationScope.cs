namespace DotPudica.Core.Threading;

/// <summary>
/// Scene/window async scope: cancel on leave so in-flight work stops updating UI.
/// Use <see cref="CreateLinkedTokenSource"/> for a child CTS (match/reconnect) that can cancel
/// without cancelling the whole scene.
/// </summary>
public sealed class SceneOperationScope : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public CancellationToken Token
    {
        get
        {
            ThrowIfDisposed();
            return _cts.Token;
        }
    }

    /// <summary>Child CTS linked to the scene token; cancelling the child does not cancel the scene.</summary>
    public CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] additionalTokens)
    {
        ThrowIfDisposed();
        if (additionalTokens.Length == 0)
            return CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

        var tokens = new CancellationToken[additionalTokens.Length + 1];
        tokens[0] = _cts.Token;
        additionalTokens.CopyTo(tokens, 1);
        return CancellationTokenSource.CreateLinkedTokenSource(tokens);
    }

    public void Cancel()
    {
        if (_disposed)
            return;

        _cts.Cancel();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cancel();
        _cts.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SceneOperationScope));
    }
}
