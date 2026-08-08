#if DOTPUDICA_DIAGNOSTICS_DEMO
// This file is only used for local manual verification of DotPudica source generator compile-time diagnostics (DOTPUDICA0xx).
// It is excluded from compilation by default (the DOTPUDICA_DIAGNOSTICS_DEMO symbol is not defined); once enabled,
// each declaration below will trigger the corresponding compile error/warning as noted — this is expected behavior, not a code defect.
// Usage: see repo Wiki/Diagnostics.md.

using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Diagnostics.BadDemo;

// ---- DOTPUDICA001: binding path cannot be resolved ----
public partial class BadPathViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "";
}

[DotPudicaView(typeof(BadPathViewModel))]
public partial class BadPathView : Control
{
    [Export, BindTo("TitleTypoDoesNotExist")]
    private Label _label = null!;
}

// ---- DOTPUDICA005: command property is not ICommand ----
// Note (known generator limitation, see Wiki/Diagnostics.md "Known Issues"):
// BindingGenerator.ResolveCommandPath returns early in the "path resolved but not ICommand" branch,
// without writing to CommandPathMembers; the diagnostic phase checks for null and therefore reports DOTPUDICA001 (path cannot be resolved)
// instead of the intended DOTPUDICA005. This example is kept to document the behavior, not to fix the generator (beyond Showcase scope).
public partial class BadCommandViewModel : ViewModelBase
{
    public string NotACommand => "I am not ICommand";
}

[DotPudicaView(typeof(BadCommandViewModel))]
public partial class BadCommandView : Control
{
    [Export, BindCommand(nameof(BadCommandViewModel.NotACommand))]
    private Button _button = null!;
}

// ---- DOTPUDICA010: collection source does not implement INotifyCollectionChanged ----
public partial class BadCollectionViewModel : ViewModelBase
{
    public List<string> PlainList { get; } = new();
}

[DotPudicaView(typeof(BadCollectionViewModel))]
public partial class BadCollectionView : Control
{
    [Export, ItemsSource(nameof(BadCollectionViewModel.PlainList), "res://not_used.tscn")]
    private VBoxContainer _list = null!;
}

// ---- DOTPUDICA046: lifecycle override must call the generated entry point ----
public partial class BadLifecycleViewModel : ViewModelBase
{
    [ObservableProperty] private string _text = "";
}

[DotPudicaView(typeof(BadLifecycleViewModel))]
public partial class BadLifecycleView : Control
{
    [Export, BindTo(nameof(BadLifecycleViewModel.Text))]
    private Label _label = null!;

    // AutoInitialize=true (default) generates _Ready/_ExitTree; overriding them directly is rejected —
    // use the OnViewReady / OnViewDisposing hooks instead.
    public override void _ExitTree()
    {
        base._ExitTree();
    }
}

// ---- DOTPUDICA040: ViewModel constructor is not DI-resolvable ----
public partial class BadFactoryViewModel : ViewModelBase
{
    public BadFactoryViewModel(int concreteNotAService) { }
}

[DotPudicaView(typeof(BadFactoryViewModel))]
public partial class BadFactoryView : Control
{
    // Constructor parameter is not an interface — the generated factory cannot resolve it;
    // declare a [ViewModelFactory] method on the view instead.
}

// ---- DOTPUDICA042: [Subscribe] event does not exist or signature mismatch ----
public partial class BadSubscribeViewModel : ViewModelBase
{
    [ObservableProperty] private string _text = "";
}

[DotPudicaView(typeof(BadSubscribeViewModel))]
public partial class BadSubscribeView : Control
{
    // Event 'NoSuchEvent' does not exist on BadSubscribeViewModel.
    [Subscribe("NoSuchEvent")]
    private void OnNoSuchEvent() { }
}

// ---- DOTPUDICA043: [Inject] target must be writable ----
public partial class BadInjectViewModel : ViewModelBase
{
}

[DotPudicaView(typeof(BadInjectViewModel))]
public partial class BadInjectView : Control
{
    // readonly field cannot be injected.
    [Inject]
    private readonly Label _notWritable = null!;
}

// ---- DOTPUDICA045: PoolSize is not applicable to virtualized items ----
public partial class BadVirtualizedViewModel : ViewModelBase
{
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<string> _items = [];
}

[DotPudicaView(typeof(BadVirtualizedViewModel))]
public partial class BadVirtualizedView : Control
{
    // VirtualizedItemsControl manages its own recycling; PoolSize is rejected.
    [Export, ItemsSource(nameof(BadVirtualizedViewModel.Items), "res://not_used.tscn", PoolSize = 8)]
    private DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl _list = null!;
}

// ---- DOTPUDICA030: binding path segment cannot be a value type ----
public struct BadStructMiddle
{
    public string Name;
}

public partial class BadStructPathViewModel : ViewModelBase
{
    public BadStructMiddle Middle { get; set; } = new() { Name = "x" };
}

[DotPudicaView(typeof(BadStructPathViewModel))]
public partial class BadStructPathView : Control
{
    [Export, BindTo("Middle.Name")]
    private Label _label = null!;
}

// ---- DOTPUDICA031: target property is invalid ----
public partial class BadTargetViewModel : ViewModelBase
{
    [ObservableProperty] private string _text = "";
}

[DotPudicaView(typeof(BadTargetViewModel))]
public partial class BadTargetView : Control
{
    [Export, BindTo(nameof(BadTargetViewModel.Text), Target = "ThisPropertyDoesNotExistOnButton")]
    private Button _button = null!;
}

// ---- DOTPUDICA032: source/target type mismatch without a typed converter ----
public partial class BadTypeMismatchViewModel : ViewModelBase
{
    [ObservableProperty] private int _score;
}

[DotPudicaView(typeof(BadTypeMismatchViewModel))]
public partial class BadTypeMismatchView : Control
{
    // int -> Control.TooltipText(string): neither same type nor implicit numeric conversion, and no converter provided.
    [Export, BindTo(nameof(BadTypeMismatchViewModel.Score), Target = "TooltipText")]
    private Control _control = null!;
}

// ---- DOTPUDICA033: converter does not implement the required typed interface ----
public sealed class BadNonTypedConverter : IValueConverter
{
    public object? Convert(object? value, System.Type targetType) => value?.ToString();
    public object? ConvertBack(object? value, System.Type targetType) => value;
}

public partial class BadConverterViewModel : ViewModelBase
{
    [ObservableProperty] private int _score;
}

[DotPudicaView(typeof(BadConverterViewModel))]
public partial class BadConverterView : Control
{
    // BadNonTypedConverter only implements the erased-type IValueConverter, not IValueConverter<int,string>.
    [Export, BindTo(nameof(BadConverterViewModel.Score), Target = "TooltipText", Converter = typeof(BadNonTypedConverter))]
    private Control _control = null!;
}

// ---- DOTPUDICA034: derived-to-base TwoWay binding requires a converter ----
public partial class BadUpcastTargetControl : Control
{
    public Resource? Texture { get; set; }

    [Signal]
    public delegate void ChangedEventHandler();
}

public partial class BadUpcastViewModel : ViewModelBase
{
    [ObservableProperty] private Texture2D _icon = null!;
}

[DotPudicaView(typeof(BadUpcastViewModel))]
public partial class BadUpcastView : Control
{
    // Texture2D (derived) -> Resource (base) used for TwoWay; runtime type safety cannot be guaranteed when writing back.
    [Export, BindTo(nameof(BadUpcastViewModel.Icon),
        Target = nameof(BadUpcastTargetControl.Texture), Mode = BindingMode.TwoWay, Signal = "changed")]
    private BadUpcastTargetControl _slot = null!;
}

// ---- DOTPUDICA035: silent boxing binding is prohibited ----
public partial class BadBoxingTargetControl : Control
{
    public object? Value { get; set; }
}

public partial class BadBoxingViewModel : ViewModelBase
{
    [ObservableProperty] private int _score;
}

[DotPudicaView(typeof(BadBoxingViewModel))]
public partial class BadBoxingView : Control
{
    // int -> object causes boxing, breaking the zero-allocation hot path.
    [Export, BindTo(nameof(BadBoxingViewModel.Score), Target = nameof(BadBoxingTargetControl.Value))]
    private BadBoxingTargetControl _host = null!;
}
#endif
