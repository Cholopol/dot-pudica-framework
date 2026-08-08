using System.Collections;

namespace DotPudica.Core.Binding;

/// <summary>
/// Virtualized list target: owns a bounded visible window, unlike
/// <see cref="IItemsTargetProxy"/> which receives one op per source item.
/// </summary>
public interface IVirtualizedItemsTargetProxy : IDisposable
{
    void SetItems(IList? items);
    void Refresh();
}
