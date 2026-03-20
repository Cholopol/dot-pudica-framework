namespace DotPudica.Core.ObjectPool;

/// <summary>
/// Thread-safe generic object pool. Uses Interlocked.CompareExchange for lock-free allocation/recycling.
/// </summary>
/// <typeparam name="T">Pooled object type</typeparam>
public class ObjectPool<T> : IObjectPool<T>, IObjectPool where T : class
{
    private readonly Entry[] _entries;
    private readonly int _initialSize;
    private readonly IObjectFactory<T> _factory;
    private bool _disposed;

    public int MaxSize { get; }
    public int InitialSize => _initialSize;

    /// <summary>
    /// Create object pool.
    /// </summary>
    /// <param name="factory">Object factory</param>
    /// <param name="initialSize">Initial pre-allocation count</param>
    /// <param name="maxSize">Pool maximum capacity</param>
    public ObjectPool(IObjectFactory<T> factory, int initialSize = 0, int maxSize = 0)
    {
        if (maxSize <= 0)
            maxSize = Environment.ProcessorCount * 2;

        if (maxSize < initialSize)
            throw new ArgumentException("maxSize must be greater than or equal to initialSize");

        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _initialSize = initialSize;
        MaxSize = maxSize;
        _entries = new Entry[maxSize];

        // Pre-allocate initial objects
        for (int i = 0; i < initialSize; i++)
        {
            _entries[i].Value = factory.Create(this);
        }
    }

    /// <summary>
    /// Get object from pool. Prioritizes reusing existing objects, creates new object via factory when pool is empty.
    /// </summary>
    public virtual T Allocate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        for (int i = 0; i < _entries.Length; i++)
        {
            var value = _entries[i].Value;
            if (value == null)
                continue;

            if (Interlocked.CompareExchange(ref _entries[i].Value, null, value) == value)
                return value;
        }

        return _factory.Create(this);
    }

    /// <summary>
    /// Return object to pool. If pool is full or object is invalid, destroy the object.
    /// </summary>
    public virtual void Free(T obj)
    {
        if (obj == null)
            return;

        if (_disposed || !_factory.Validate(obj))
        {
            _factory.Destroy(obj);
            return;
        }

        _factory.Reset(obj);

        for (int i = 0; i < _entries.Length; i++)
        {
            if (Interlocked.CompareExchange(ref _entries[i].Value, obj, null) == null)
                return;
        }

        // Pool is full, destroy object
        _factory.Destroy(obj);
    }

    object IObjectPool.Allocate() => Allocate();
    void IObjectPool.Free(object obj) => Free((T)obj);

    /// <summary>
    /// Clear all objects in the pool.
    /// </summary>
    protected virtual void Clear()
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            var value = Interlocked.Exchange(ref _entries[i].Value, null);
            if (value != null)
                _factory.Destroy(value);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }

    ~ObjectPool()
    {
        Dispose(false);
    }

    private struct Entry
    {
        public T? Value;
    }
}
