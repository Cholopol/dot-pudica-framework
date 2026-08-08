using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace DotPudica.Integration.Fixtures;

public partial class IntegrationListViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _items = new() { "a", "b" };
}
