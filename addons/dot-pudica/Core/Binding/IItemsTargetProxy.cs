namespace DotPudica.Core.Binding;

/// <summary>Collection container adapter for <see cref="CollectionBinding"/> child-node ops.</summary>
public interface IItemsTargetProxy : IDisposable
{
    void Add(object? item, int index);
    void RemoveAt(int index);
    void Move(int oldIndex, int newIndex);
    void Clear();
}
