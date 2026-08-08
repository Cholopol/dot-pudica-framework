using DotPudica.Core.ObjectPool;

namespace DotPudica.Tests;

public class ObjectPoolTests
{
    [Fact]
    public void Allocate_ThenFree_ReusesSameInstance()
    {
        var factory = new TrackingFactory();
        var pool = new ObjectPool<PooledItem>(factory, initialSize: 1, maxSize: 2);

        var first = pool.Allocate();
        pool.Free(first);
        var second = pool.Allocate();

        Assert.Same(first, second);
        Assert.Equal(1, factory.CreatedCount);
        Assert.Equal(1, factory.ResetCount);
    }

    [Fact]
    public void Free_WhenInvalid_DestroysInsteadOfPooling()
    {
        var factory = new TrackingFactory { ValidateResult = false };
        var pool = new ObjectPool<PooledItem>(factory, initialSize: 0, maxSize: 2);

        var item = pool.Allocate();
        pool.Free(item);

        Assert.Equal(1, factory.DestroyedCount);
        Assert.Equal(0, factory.ResetCount);
    }

    [Fact]
    public void Dispose_DestroysRetainedPooledObjects()
    {
        var factory = new TrackingFactory();
        var pool = new ObjectPool<PooledItem>(factory, initialSize: 1, maxSize: 1);

        pool.Dispose();

        Assert.Equal(1, factory.DestroyedCount);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var factory = new TrackingFactory();
        var pool = new ObjectPool<PooledItem>(factory, initialSize: 1, maxSize: 1);

        pool.Dispose();
        pool.Dispose();

        Assert.Equal(1, factory.DestroyedCount);
    }

    private sealed class PooledItem;

    private sealed class TrackingFactory : IObjectFactory<PooledItem>
    {
        public int CreatedCount { get; private set; }
        public int DestroyedCount { get; private set; }
        public int ResetCount { get; private set; }
        public bool ValidateResult { get; set; } = true;

        public PooledItem Create(IObjectPool<PooledItem> pool)
        {
            CreatedCount++;
            return new PooledItem();
        }

        public void Reset(PooledItem obj) => ResetCount++;

        public bool Validate(PooledItem obj) => ValidateResult;

        public void Destroy(PooledItem obj) => DestroyedCount++;
    }
}
