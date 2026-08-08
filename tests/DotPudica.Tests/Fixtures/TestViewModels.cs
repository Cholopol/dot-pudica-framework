using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace DotPudica.Tests.Fixtures;

/// <summary>
/// Simple ViewModel for single-segment property binding tests.
/// </summary>
public sealed class SimpleViewModel : INotifyPropertyChanged
{
    private string _name = "";
    private int _age;
    private bool _isActive;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public int Age
    {
        get => _age;
        set => SetField(ref _age, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Nested model for chained path tests.
/// </summary>
public sealed class NestedModel : INotifyPropertyChanged
{
    private string _value = "";
    private int _count;
    private NestedChild? _child;

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    public NestedChild? Child
    {
        get => _child;
        set => SetField(ref _child, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class NestedChild : INotifyPropertyChanged
{
    private string _text = "";

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// ViewModel with nested properties for deep path tests.
/// </summary>
public sealed class NestedViewModel : INotifyPropertyChanged
{
    private NestedModel _model = new();
    private SimpleViewModel _child = new();

    public NestedModel Model
    {
        get => _model;
        set => SetField(ref _model, value);
    }

    public SimpleViewModel Child
    {
        get => _child;
        set => SetField(ref _child, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// ViewModel with commands for command binding tests.
/// </summary>
public sealed class CommandViewModel : INotifyPropertyChanged
{
    private int _executeCount;
    private ICommand? _command;
    private string _parameter = "";

    public int ExecuteCount
    {
        get => _executeCount;
        set => SetField(ref _executeCount, value);
    }

    public ICommand? Command
    {
        get => _command;
        set => SetField(ref _command, value);
    }

    public string Parameter
    {
        get => _parameter;
        set => SetField(ref _parameter, value);
    }

    public ICommand DefaultCommand => new RelayCommand(() => ExecuteCount++);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// ViewModel with RelayCommand, testing [RelayCommand] compatibility.
/// </summary>
public sealed partial class RelayCommandViewModel
{
    [RelayCommand]
    public void Save() => Saved = true;

    [RelayCommand]
    public Task LoadAsync() => Task.CompletedTask;

    public bool Saved { get; set; }
}

/// <summary>
/// ViewModel with ObservableCollection for collection binding tests.
/// </summary>
public sealed class CollectionViewModel : INotifyPropertyChanged
{
    private ObservableCollection<string> _items = new();

    public ObservableCollection<string> Items
    {
        get => _items;
        set => SetField(ref _items, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
