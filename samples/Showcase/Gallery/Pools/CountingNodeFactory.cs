using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using Godot;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>
/// NodeFactory wrapper exposing deterministic Create/Reset/Destroy counters for pool stats —
/// destroyed is exact at the Free() call (no QueueFree frame lag), unlike sampling IsInstanceValid.
/// </summary>
public sealed class CountingNodeFactory<T> : IObjectFactory<T> where T : Node, new()
{
    private readonly NodeFactory<T> _inner = new();

    public int CreatedCount { get; private set; }
    public int DestroyedCount { get; private set; }

    public T Create(IObjectPool<T> pool)
    {
        CreatedCount++;
        return _inner.Create(pool);
    }

    public void Reset(T obj) => _inner.Reset(obj);

    public bool Validate(T obj) => _inner.Validate(obj);

    public void Destroy(T obj)
    {
        _inner.Destroy(obj);
        DestroyedCount++;
    }
}
