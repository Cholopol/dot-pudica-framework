using DotPudica.Core.ObjectPool;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>Simple <see cref="IObjectFactory{T}"/> implementation that tracks create/recycle/destroy counts for UI display.</summary>
public sealed class PoolItemFactory : IObjectFactory<PoolItem>
{
    public int CreatedCount { get; private set; }
    public int ResetCalledCount { get; private set; }
    public int DestroyedCount { get; private set; }

    public PoolItem Create(IObjectPool<PoolItem> pool)
    {
        CreatedCount++;
        return new PoolItem();
    }

    public void Reset(PoolItem obj)
    {
        ResetCalledCount++;
        obj.ResetCount++;
    }

    public bool Validate(PoolItem obj) => true;

    public void Destroy(PoolItem obj) => DestroyedCount++;
}
