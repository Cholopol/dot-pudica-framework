namespace DotPudica.Core.Threading;

/// <summary>
/// Latest-wins mailbox: background writers replace the immutable snapshot; UI drains once and applies.
/// Do not mutate ItemsSource-bound ObservableCollections item-by-item off the UI thread.
/// </summary>
public sealed class LatestSnapshotMailbox<T>
{
    private readonly object _gate = new();
    private T? _latest;
    private bool _hasValue;

    public void Publish(T immutableSnapshot)
    {
        if (!typeof(T).IsValueType)
            ArgumentNullException.ThrowIfNull(immutableSnapshot);
        lock (_gate)
        {
            _latest = immutableSnapshot;
            _hasValue = true;
        }
    }

    /// <summary>Take and clear the latest snapshot; false if empty.</summary>
    public bool TryDrainLatest(out T? snapshot)
    {
        lock (_gate)
        {
            if (!_hasValue)
            {
                snapshot = default;
                return false;
            }

            snapshot = _latest;
            _latest = default;
            _hasValue = false;
            return true;
        }
    }
}
