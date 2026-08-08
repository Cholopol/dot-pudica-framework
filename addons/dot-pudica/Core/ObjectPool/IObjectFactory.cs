namespace DotPudica.Core.ObjectPool;

/// <summary>Create / reset / validate / destroy hooks for <see cref="ObjectPool{T}"/>.</summary>
public interface IObjectFactory<T> where T : class
{
    T Create(IObjectPool<T> pool);
    void Reset(T obj);
    bool Validate(T obj);
    void Destroy(T obj);
}
