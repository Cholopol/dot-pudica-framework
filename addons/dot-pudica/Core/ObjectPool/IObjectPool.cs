namespace DotPudica.Core.ObjectPool;

/// <summary>
/// Object pool interface. Provides object allocation and recycling functionality.
/// </summary>
public interface IObjectPool<T> : IDisposable where T : class
{
    /// <summary>
    /// Get an object from the pool. If no available object in pool, create new object via factory.
    /// </summary>
    T Allocate();

    /// <summary>
    /// Return object to pool. If pool is full, destroy the object.
    /// </summary>
    void Free(T obj);

    /// <summary>
    /// Maximum capacity of the pool.
    /// </summary>
    int MaxSize { get; }
}

/// <summary>
/// Non-generic object pool interface.
/// </summary>
public interface IObjectPool : IDisposable
{
    /// <summary>
    /// Get an object from the pool.
    /// </summary>
    object Allocate();

    /// <summary>
    /// Return object to pool.
    /// </summary>
    void Free(object obj);
}
