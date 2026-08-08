using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>Per-item ViewModel for <see cref="PooledDetailPanel"/>; owned by PoolsPage, never by the panel.</summary>
public partial class PooledDetailViewModel : ViewModelBase
{
    public PooledDetailViewModel(int index)
    {
        Title = $"Detail #{index}";
        DetailText = $"Created at allocate time — the panel is reused, the ViewModel is not.";
    }

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _detailText = "";
}
