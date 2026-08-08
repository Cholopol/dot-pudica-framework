using System.ComponentModel;

namespace DotPudica.Core.Binding;

public interface IBindingPath<TValue> : IDisposable
{
    void Bind(object? source);
    void Unbind();
    TValue GetValue();
    bool SetValue(TValue value);
    event EventHandler? ValueChanged;
}

/// <summary>
/// Type-erased path, separate from <see cref="IBindingPath{TValue}"/> to avoid CS0695 when a class implements both.
/// </summary>
public interface IBindingPath : IDisposable
{
    void Bind(object? source);
    void Unbind();
    object? GetValue();
    bool SetValue(object? value);
    event EventHandler? ValueChanged;
}

/// <summary>
/// Compile-time delegates + INPC chain; no expression trees or reflection (AOT/trimming safe).
/// Intermediate path segments must be reference types (enforced by the source generator).
/// </summary>
public sealed class TypedBindingPath<TSource, TValue> : IBindingPath<TValue>, IBindingPath
    where TSource : class
{
    private readonly Func<TSource, TValue> _getter;
    private readonly Action<TSource, TValue>? _setter;
    private readonly Func<TSource, object?>[] _prefixGetters;
    private readonly string[] _segments;
    private readonly List<PathNode> _nodes = new();
    private TSource? _source;
    private bool _disposed;

    public event EventHandler? ValueChanged;

    public TypedBindingPath(
        Func<TSource, TValue> getter,
        Action<TSource, TValue>? setter,
        string[] segments,
        Func<TSource, object?>[]? prefixGetters = null)
    {
        _getter = getter;
        _setter = setter;
        _segments = segments;
        if (_segments.Length == 0)
            throw new ArgumentException("A binding path requires at least one property name segment.", nameof(segments));

        _prefixGetters = prefixGetters ?? Array.Empty<Func<TSource, object?>>();
        if (_segments.Length > 1 && _prefixGetters.Length < _segments.Length - 1)
        {
            throw new ArgumentException(
                $"Nested path requires at least {_segments.Length - 1} prefix getters, but got {_prefixGetters.Length}.",
                nameof(prefixGetters));
        }
    }

    public void Bind(object? source)
    {
        Unbind();
        if (source is not TSource t)
            return;

        _source = t;
        RebuildChain(t);
    }

    public TValue GetValue()
    {
        if (_source is null || HasNullIntermediate(_source))
            return default!;

        return _getter(_source);
    }

    public bool SetValue(TValue value)
    {
        if (_setter is null || _source is null || HasNullIntermediate(_source))
            return false;

        _setter(_source, value);
        return true;
    }

    private bool HasNullIntermediate(TSource source)
    {
        for (var i = 0; i < _segments.Length - 1; i++)
        {
            if (_prefixGetters[i](source) is null)
                return true;
        }
        return false;
    }

    object? IBindingPath.GetValue() => GetValue();

    bool IBindingPath.SetValue(object? value)
    {
        if (_setter is null || _source is null)
            return false;

        TValue converted;
        if (value is TValue typed)
            converted = typed;
        else if (value is null)
            converted = default!;
        else
            return false;

        return SetValue(converted);
    }

    public void Unbind()
    {
        foreach (var node in _nodes)
            node.Unsubscribe();
        _nodes.Clear();
        _source = null;
    }

    private void RebuildChain(TSource source)
    {
        object? current = source;
        for (var i = 0; i < _segments.Length; i++)
        {
            if (current is null)
                break;

            var node = new PathNode(current, _segments[i], i);
            node.PropertyChanged += OnPathNodePropertyChanged;
            _nodes.Add(node);

            if (i < _segments.Length - 1)
                current = _prefixGetters[i](source);
        }
    }

    private void OnPathNodePropertyChanged(object? sender, int segmentIndex)
    {
        for (var i = _nodes.Count - 1; i > segmentIndex; i--)
        {
            _nodes[i].Unsubscribe();
            _nodes.RemoveAt(i);
        }

        if (_source is null)
            return;

        // Leaf change: notify without rebuilding the chain
        if (segmentIndex >= _segments.Length - 1)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        object? current = _prefixGetters[segmentIndex](_source);
        for (var i = segmentIndex + 1; i < _segments.Length; i++)
        {
            if (current is null)
                break;

            var node = new PathNode(current, _segments[i], i);
            node.PropertyChanged += OnPathNodePropertyChanged;
            _nodes.Add(node);

            if (i < _segments.Length - 1)
                current = _prefixGetters[i](_source);
        }

        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unbind();
            _disposed = true;
        }
    }

    private sealed class PathNode
    {
        private readonly string _propertyName;
        private readonly int _segmentIndex;
        private INotifyPropertyChanged? _observable;

        public object? Source { get; private set; }

        public event Action<object?, int>? PropertyChanged;

        public PathNode(object source, string propertyName, int segmentIndex)
        {
            Source = source;
            _propertyName = propertyName;
            _segmentIndex = segmentIndex;

            if (source is INotifyPropertyChanged npc)
            {
                _observable = npc;
                _observable.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is null || e.PropertyName == _propertyName)
                PropertyChanged?.Invoke(this, _segmentIndex);
        }

        public void Unsubscribe()
        {
            if (_observable is not null)
            {
                _observable.PropertyChanged -= OnPropertyChanged;
                _observable = null;
            }
            Source = null;
            PropertyChanged = null;
        }
    }
}
