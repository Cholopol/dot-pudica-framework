namespace DotPudica.Core.ObjectPool;

public interface IObjectPool<T> : IDisposable where T : class
{
    T Allocate();
    void Free(T obj);
    int MaxSize { get; }
}

public interface IObjectPool : IDisposable
{
    object Allocate();
    void Free(object obj);
}
