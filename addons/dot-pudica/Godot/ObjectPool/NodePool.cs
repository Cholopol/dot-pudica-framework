using DotPudica.Core.ObjectPool;
using Godot;

namespace DotPudica.Godot.ObjectPool;

public class NodeFactory<T> : IObjectFactory<T> where T : Node, new()
{
    public T Create(IObjectPool<T> pool) => new T();

    public void Reset(T obj) => obj.GetParent()?.RemoveChild(obj);

    public bool Validate(T obj) => GodotObject.IsInstanceValid(obj);

    public void Destroy(T obj) => obj.QueueFree();
}

/// <summary>
/// Instantiates via <see cref="PackedScene"/>. For nodes that frequently enter and leave the scene.
/// </summary>
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

    public void Reset(T obj) => obj.GetParent()?.RemoveChild(obj);

    public bool Validate(T obj) => GodotObject.IsInstanceValid(obj);

    public void Destroy(T obj) => obj.QueueFree();
}

/// <summary>
/// Godot-side object pool facade: wraps Core's <see cref="ObjectPool{T}"/> with Node/Scene factories.
/// The pool algorithm lives in <c>DotPudica.Core.ObjectPool</c>; this class only handles Node creation, tree removal, validation, and QueueFree.
/// </summary>
public static class NodePool
{
    public static IObjectPool<Node> Create(PackedScene scene, int maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize));

        return new Core.ObjectPool.ObjectPool<Node>(new SceneFactory<Node>(scene), 0, maxSize);
    }

    public static IObjectPool<T> Create<T>(int maxSize = 0) where T : Node, new()
        => new Core.ObjectPool.ObjectPool<T>(new NodeFactory<T>(), 0, maxSize);

    public static IObjectPool<T> Create<T>(PackedScene scene, int maxSize = 0) where T : Node
        => new Core.ObjectPool.ObjectPool<T>(new SceneFactory<T>(scene), 0, maxSize);

    public static IObjectPool<T> Create<T>(string scenePath, int maxSize = 0) where T : Node
        => new Core.ObjectPool.ObjectPool<T>(new SceneFactory<T>(scenePath), 0, maxSize);
}
