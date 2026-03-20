namespace DotPudica.Core.ObjectPool;

/// <summary>
/// Object factory interface. Used for creating, resetting, validating and destroying objects in the object pool.
/// </summary>
/// <typeparam name="T">Pooled object type</typeparam>
public interface IObjectFactory<T> where T : class
{
    /// <summary>
    /// Create new object.
    /// </summary>
    /// <param name="pool">The object pool this object belongs to</param>
    T Create(IObjectPool<T> pool);

    /// <summary>
    /// Reset object state (called before returning to pool).
    /// </summary>
    void Reset(T obj);

    /// <summary>
    /// Validate whether the object is still valid (invalid objects will not be returned to pool).
    /// </summary>
    bool Validate(T obj);

    /// <summary>
    /// Destroy object (called when pool is full or pool is disposed).
    /// </summary>
    void Destroy(T obj);
}
