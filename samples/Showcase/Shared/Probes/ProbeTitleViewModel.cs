using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Shared.Probes;

/// <summary>Minimal ViewModel for probe driving (shared by Gallery and Integration).</summary>
public partial class ProbeTitleViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "";
}
