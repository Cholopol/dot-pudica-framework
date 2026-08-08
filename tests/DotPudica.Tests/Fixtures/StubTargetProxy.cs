using DotPudica.Core.Binding;

namespace DotPudica.Tests.Fixtures;

/// <summary>
/// Test binding path factory, avoids manually writing segment name / prefix array boilerplate.
/// </summary>
public static class BindingPathFactory
{
    public static TypedBindingPath<TSource, TValue> Create<TSource, TValue>(
        Func<TSource, TValue> getter,
        Action<TSource, TValue>? setter,
        params string[] segments)
        where TSource : class
    {
        if (segments.Length == 0)
            throw new ArgumentException("At least one path segment is required.", nameof(segments));

        Func<TSource, object?>[]? prefixes = null;
        if (segments.Length > 1)
            throw new ArgumentException("Multi-segment paths should use CreateNested.", nameof(segments));

        return new TypedBindingPath<TSource, TValue>(getter, setter, segments, prefixes);
    }

    public static TypedBindingPath<TSource, TValue> CreateNested<TSource, TValue>(
        Func<TSource, TValue> getter,
        Action<TSource, TValue>? setter,
        string[] segments,
        Func<TSource, object?>[] prefixGetters)
        where TSource : class
        => new(getter, setter, segments, prefixGetters);
}

/// <summary>
/// ITargetProxy test stub. Records all GetValue/SetValue calls.
/// </summary>
public sealed class StubTargetProxy : ITargetProxy
{
    private object? _value;
    private bool _disposed;

    public int GetValueCallCount { get; private set; }
    public int SetValueCallCount { get; private set; }
    public List<object?> SetValues { get; } = new();

    public event EventHandler? ValueChanged;

    public StubTargetProxy(object? initialValue = null)
    {
        _value = initialValue;
    }

    public object? GetValue()
    {
        GetValueCallCount++;
        return _value;
    }

    public void SetValue(object? value)
    {
        SetValueCallCount++;
        SetValues.Add(value);
        _value = value;
    }

    public void SimulateUserInput(object? newValue)
    {
        _value = newValue;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        ValueChanged = null;
        _disposed = true;
    }
}

/// <summary>
/// Typed target proxy test stub.
/// </summary>
public sealed class StubTargetProxy<TValue> : ITypedTargetProxy<TValue>
{
    private TValue _value;
    private bool _disposed;

    public int GetValueCallCount { get; private set; }
    public int SetValueCallCount { get; private set; }
    public List<TValue> SetValues { get; } = new();
    public TValue Value => _value;

    public event EventHandler? ValueChanged;

    public StubTargetProxy(TValue initialValue = default!)
    {
        _value = initialValue;
    }

    public TValue GetValue()
    {
        GetValueCallCount++;
        return _value;
    }

    public void SetValue(TValue value)
    {
        SetValueCallCount++;
        SetValues.Add(value);
        _value = value;
    }

    public void SimulateUserInput(TValue newValue)
    {
        _value = newValue;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        ValueChanged = null;
        _disposed = true;
    }
}

/// <summary>
/// Simple value converter that reverses strings.
/// </summary>
public sealed class ReverseStringConverter : IValueConverter, IValueConverter<string, string>
{
    public object? Convert(object? value, Type targetType)
        => value is string s ? new string(s.Reverse().ToArray()) : value;

    public object? ConvertBack(object? value, Type targetType)
        => value is string s ? new string(s.Reverse().ToArray()) : value;

    string IValueConverter<string, string>.Convert(string value)
        => new(value.Reverse().ToArray());

    string IValueConverter<string, string>.ConvertBack(string value)
        => new(value.Reverse().ToArray());
}

/// <summary>
/// int → string converter.
/// </summary>
public sealed class IntToTextConverter : IValueConverter, IValueConverter<int, string>
{
    public object? Convert(object? value, Type targetType)
        => value is int i ? $"Value: {i}" : value?.ToString();

    public object? ConvertBack(object? value, Type targetType)
        => int.TryParse(value?.ToString(), out var result) ? result : 0;

    string IValueConverter<int, string>.Convert(int value)
        => $"Value: {value}";

    int IValueConverter<int, string>.ConvertBack(string value)
        => int.TryParse(value, out var result) ? result : 0;
}

/// <summary>Manual dispatcher used to verify that bindings do not touch targets before the UI queue runs.</summary>
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
