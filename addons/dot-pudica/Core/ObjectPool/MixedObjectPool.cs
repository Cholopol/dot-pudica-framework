using System.Collections.Concurrent;

namespace DotPudica.Core.ObjectPool;

/// <summary>
/// Mixed-type object pool. Manages pooling of different object types by type key.
/// </summary>
/// <typeparam name="T">Base type of pooled objects</typeparam>
public class MixedObjectPool<T> : IDisposable where T : class
{
    private readonly ConcurrentDictionary<string, IObjectPool<T>> _pools = new();
    private readonly Func<string, IObjectFactory<T>> _factoryProvider;
    private readonly int _maxSizePerType;
    private bool _disposed;

    /// <summary>
    /// Create mixed object pool.
    /// </summary>
    /// <param name="factoryProvider">Delegate to create corresponding factory based on type name</param>
    /// <param name="maxSizePerType">Maximum pool capacity per type</param>
    public MixedObjectPool(Func<string, IObjectFactory<T>> factoryProvider, int maxSizePerType = 0)
    {
        _factoryProvider = factoryProvider ?? throw new ArgumentNullException(nameof(factoryProvider));
        _maxSizePerType = maxSizePerType;
    }

    /// <summary>
    /// Allocate object from the pool of specified type.
    /// </summary>
    public T Allocate(string typeName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pool = _pools.GetOrAdd(typeName,
            key => new ObjectPool<T>(_factoryProvider(key), 0, _maxSizePerType));

        return pool.Allocate();
    }

    /// <summary>
    /// Return object to the pool of specified type.
    /// </summary>
    public void Free(string typeName, T obj)
    {
        if (obj == null)
            return;

        if (_pools.TryGetValue(typeName, out var pool))
        {
            pool.Free(obj);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var pool in _pools.Values)
            {
                pool.Dispose();
            }
            _pools.Clear();
            _disposed = true;
        }
    }
}
