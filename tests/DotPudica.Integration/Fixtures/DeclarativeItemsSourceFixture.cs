using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Fixtures;

public partial class DeclarativeItemsSourceViewModel : ViewModelBase
{
    public ObservableCollection<string> Items { get; } = new();

    public List<string> SelectedItems { get; } = new();

    [RelayCommand]
    private void SelectItem(string item) => SelectedItems.Add(item);
}

/// <summary>
/// Declarative ItemsSource golden fixture: source-generated [ItemsSource] binding with ItemCommand propagation and pooling.
/// </summary>
[DotPudicaView(typeof(DeclarativeItemsSourceViewModel))]
public partial class DeclarativeItemsSourceView : Control
{
    [Export, ItemsSource(nameof(DeclarativeItemsSourceViewModel.Items),
        "res://tests/DotPudica.Integration/Fixtures/IntegrationItemCommand.tscn",
        PoolSize = 4,
        ItemCommand = nameof(DeclarativeItemsSourceViewModel.SelectItemCommand))]
    private VBoxContainer _list = null!;

    public VBoxContainer List => _list;
    public DeclarativeItemsSourceViewModel? PanelViewModel => ViewModel;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady()
    {
        _list ??= new VBoxContainer { Name = "ItemsList" };
        if (_list.GetParent() is null)
            AddChild(_list);
    }
}
