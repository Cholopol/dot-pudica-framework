using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.ReflectionProxy;

public partial class ReflectionProxySampleViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string _sampleText = "Initial control text";

    public string StatusText => $"ViewModel text: {SampleText}";
}
