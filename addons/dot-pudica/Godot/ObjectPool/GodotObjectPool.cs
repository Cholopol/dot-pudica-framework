using DotPudica.Core.ObjectPool;
using Godot;

namespace DotPudica.Godot.ObjectPool;

/// <summary>
/// Godot Node object factory. Creates node instances via new().
/// </summary>
/// <typeparam name="T">Node derived type</typeparam>
public class NodeFactory<T> : IObjectFactory<T> where T : Node, new()
{
    public T Create(IObjectPool<T> pool) => new T();

    public void Reset(T obj)
    {
        // Remove from parent (but don't destroy)
        obj.GetParent()?.RemoveChild(obj);
    }

    public bool Validate(T obj) => GodotObject.IsInstanceValid(obj);

    public void Destroy(T obj) => obj.QueueFree();
}

/// <summary>
/// Godot PackedScene object factory. Creates nodes by instantiating prefab scenes.
/// Suitable for frequently created/destroyed scene nodes like UI list items, bullet comments, particles, etc.
/// </summary>
/// <typeparam name="T">Node derived type</typeparam>
public class SceneFactory<T> : IObjectFactory<T> where T : Node
{
    private readonly PackedScene _scene;

    public SceneFactory(PackedScene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    public SceneFactory(string scenePath)
    {
        _scene = GD.Load<PackedScene>(scenePath)
            ?? throw new ArgumentException($"Cannot load scene: {scenePath}");
    }

    public T Create(IObjectPool<T> pool) => _scene.Instantiate<T>();

    public void Reset(T obj)
    {
        obj.GetParent()?.RemoveChild(obj);
    }

    public bool Validate(T obj) => GodotObject.IsInstanceValid(obj);

    public void Destroy(T obj) => obj.QueueFree();
}

/// <summary>
/// Godot Node object pool convenient API.
/// </summary>
public static class NodePool
{
    /// <summary>
    /// Create Node object pool (via new() construction).
    /// </summary>
    public static IObjectPool<T> Create<T>(int maxSize = 0) where T : Node, new()
    {
        return new Core.ObjectPool.ObjectPool<T>(new NodeFactory<T>(), 0, maxSize);
    }

    /// <summary>
    /// Create Node object pool (via PackedScene instantiation).
    /// </summary>
    public static IObjectPool<T> Create<T>(PackedScene scene, int maxSize = 0) where T : Node
    {
        return new Core.ObjectPool.ObjectPool<T>(new SceneFactory<T>(scene), 0, maxSize);
    }

    /// <summary>
    /// Create Node object pool (via scene path loading).
    /// </summary>
    public static IObjectPool<T> Create<T>(string scenePath, int maxSize = 0) where T : Node
    {
        return new Core.ObjectPool.ObjectPool<T>(new SceneFactory<T>(scenePath), 0, maxSize);
    }
}
