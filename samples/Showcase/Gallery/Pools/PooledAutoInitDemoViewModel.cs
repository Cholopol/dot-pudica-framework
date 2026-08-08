using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>Per-activation ViewModel for <see cref="PooledAutoInitDemoPanel"/>; Owned by the view, disposed on recycle.</summary>
public partial class PooledAutoInitDemoViewModel : ViewModelBase
{
    private static int _nextInstance;

    public PooledAutoInitDemoViewModel()
        => Title = $"Auto VM #{++_nextInstance}";

    [ObservableProperty]
    private string _title = "";
}
