using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace DotPudica.Integration.Fixtures;

public partial class IntegrationTitleViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "initial";
}
