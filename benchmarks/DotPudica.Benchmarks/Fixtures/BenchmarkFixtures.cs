using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks.Fixtures;

public sealed class IntViewModel : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs ValueChangedArgs = new(nameof(Value));
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            _value = value;
            PropertyChanged?.Invoke(this, ValueChangedArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class StringViewModel : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs NameChangedArgs = new(nameof(Name));
    private string _name = "";

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;
            _name = value;
            PropertyChanged?.Invoke(this, NameChangedArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class CollectionViewModel
{
    public ObservableCollection<string> Items { get; } = new();
}

public sealed class ZeroAllocIntProxy : ITypedTargetProxy<int>
{
    public int Value { get; set; }

    event EventHandler? ITypedTargetProxy<int>.ValueChanged
    {
        add { }
        remove { }
    }

    public int GetValue() => Value;
    public void SetValue(int value) => Value = value;
    public void Dispose() { }
}

public sealed class CountingIntProxy : ITypedTargetProxy<int>
{
    public int Value { get; set; }
    public int SetValueCallCount { get; private set; }

    event EventHandler? ITypedTargetProxy<int>.ValueChanged
    {
        add { }
        remove { }
    }

    public int GetValue() => Value;

    public void SetValue(int value)
    {
        SetValueCallCount++;
        Value = value;
    }

    public void Dispose() { }
}

public sealed class CountingStringProxy : ITypedTargetProxy<string>
{
    public string Value { get; set; } = "";
    public int SetValueCallCount { get; private set; }

    event EventHandler? ITypedTargetProxy<string>.ValueChanged
    {
        add { }
        remove { }
    }

    public string GetValue() => Value;

    public void SetValue(string value)
    {
        SetValueCallCount++;
        Value = value;
    }

    public void Dispose() { }
}

public sealed class ObjectTargetProxy : ITargetProxy
{
    private object? _value;

    public object? GetValue() => _value;
    public void SetValue(object? value) => _value = value;

    public event EventHandler? ValueChanged
    {
        add { }
        remove { }
    }

    public void Dispose() { }
}

public sealed class StubItemsTargetProxy : IItemsTargetProxy
{
    private readonly List<object?> _items = new();

    public int Count => _items.Count;

    public void Add(object? item, int index) => _items.Insert(index, item);

    public void RemoveAt(int index)
    {
        if (index >= 0 && index < _items.Count)
            _items.RemoveAt(index);
    }

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _items.Count)
            return;
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
    }

    public void Clear() => _items.Clear();

    public void Dispose() => _items.Clear();
}

public sealed class QueuedUiDispatcher : IUiDispatcher
{
    private readonly Queue<Action> _actions = new();

    public bool HasAccess { get; set; } = true;

    public bool CheckAccess() => HasAccess;

    public void Post(Action action) => _actions.Enqueue(action);

    public int PendingCount => _actions.Count;

    public void RunAll()
    {
        while (_actions.Count > 0)
            _actions.Dequeue().Invoke();
    }
}
