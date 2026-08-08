namespace Samples.Showcase.Gallery.Pools;

/// <summary>Pure CLR object pool demo object (not a Godot Node).</summary>
public sealed class PoolItem
{
    private static int _sequence;

    public PoolItem() => Id = ++_sequence;

    public int Id { get; }

    /// <summary>Number of times Reset was called by the factory, used to prove the recycle path is actually invoked.</summary>
    public int ResetCount { get; internal set; }
}
