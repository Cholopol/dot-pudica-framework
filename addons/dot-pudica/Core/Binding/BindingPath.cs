using System.ComponentModel;
using System.Reflection;

namespace DotPudica.Core.Binding;

/// <summary>
/// Binding path resolver. Supports nested property paths (e.g., "Account.Username"),
/// chain-listens to PropertyChanged events on each layer of the path,
/// automatically re-evaluates when any node in the path changes.
/// </summary>
public class BindingPath : IDisposable
{
    private readonly string _fullPath;
    private readonly string[] _segments;
    private readonly List<PathNode> _nodes = new();
    private bool _disposed;

    /// <summary>
    /// Triggered when the final property value changes.
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Full path string.
    /// </summary>
    public string FullPath => _fullPath;

    public BindingPath(string path)
    {
        _fullPath = path ?? throw new ArgumentNullException(nameof(path));
        _segments = path.Split('.');
        if (_segments.Length == 0)
            throw new ArgumentException("Path cannot be empty.", nameof(path));
    }

    /// <summary>
    /// Bind to the specified source object and start listening for property changes.
    /// </summary>
    public void Bind(object? source)
    {
        Unbind();

        if (source == null)
            return;

        RebuildChain(source);
    }

    /// <summary>
    /// Get the value currently pointed to by the path from the source object.
    /// </summary>
    public object? GetValue(object? source)
    {
        if (source == null)
            return null;

        var current = source;
        foreach (var segment in _segments)
        {
            if (current == null)
                return null;

            var prop = current.GetType().GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                return null;

            current = prop.GetValue(current);
        }
        return current;
    }

    /// <summary>
    /// Set the property value pointed to by the path in the source object (only the last segment).
    /// </summary>
    public bool SetValue(object? source, object? value)
    {
        if (source == null)
            return false;

        var current = source;

        // Traverse to the second-to-last segment to get the parent object of the final property
        for (int i = 0; i < _segments.Length - 1; i++)
        {
            if (current == null)
                return false;

            var prop = current.GetType().GetProperty(_segments[i],
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                return false;

            current = prop.GetValue(current);
        }

        if (current == null)
            return false;

        // Set the last segment property
        var lastSegment = _segments[^1];
        var lastProp = current.GetType().GetProperty(lastSegment,
            BindingFlags.Public | BindingFlags.Instance);
        if (lastProp == null || !lastProp.CanWrite)
            return false;

        var convertedValue = ConvertToType(value, lastProp.PropertyType);
        lastProp.SetValue(current, convertedValue);
        return true;
    }

    /// <summary>
    /// Get the type of the final property.
    /// </summary>
    public Type? GetValueType(object? source)
    {
        if (source == null)
            return null;

        var currentType = source.GetType();
        foreach (var segment in _segments)
        {
            var prop = currentType.GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                return null;
            currentType = prop.PropertyType;
        }
        return currentType;
    }

    /// <summary>
    /// Disconnect all property listeners.
    /// </summary>
    public void Unbind()
    {
        foreach (var node in _nodes)
        {
            node.Unsubscribe();
        }
        _nodes.Clear();
    }

    /// <summary>
    /// Rebuild the listener chain. Starting from the source object, listen to PropertyChanged for each segment property layer by layer.
    /// </summary>
    private void RebuildChain(object source)
    {
        Unbind();

        var current = source;
        for (int i = 0; i < _segments.Length; i++)
        {
            if (current == null)
                break;

            var segmentIndex = i;
            var node = new PathNode(current, _segments[i], segmentIndex);
            node.PropertyChanged += OnPathNodePropertyChanged;
            _nodes.Add(node);

            // Get the value of the current segment as the source for the next segment
            var prop = current.GetType().GetProperty(_segments[i],
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && i < _segments.Length - 1)
            {
                current = prop.GetValue(current);
            }
        }
    }

    /// <summary>
    /// When a property in the path changes, rebind subsequent segments and notify that the final value has changed.
    /// </summary>
    private void OnPathNodePropertyChanged(object? sender, int segmentIndex)
    {
        // Remove old listeners after the changed segment
        for (int i = _nodes.Count - 1; i > segmentIndex; i--)
        {
            _nodes[i].Unsubscribe();
            _nodes.RemoveAt(i);
        }

        // Rebuild subsequent chain starting from the changed segment
        var current = _nodes[segmentIndex].Source;
        var prop = current?.GetType().GetProperty(_segments[segmentIndex],
            BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            var nextValue = prop.GetValue(current);
            for (int i = segmentIndex + 1; i < _segments.Length; i++)
            {
                if (nextValue == null)
                    break;

                var node = new PathNode(nextValue, _segments[i], i);
                node.PropertyChanged += OnPathNodePropertyChanged;
                _nodes.Add(node);

                var nextProp = nextValue.GetType().GetProperty(_segments[i],
                    BindingFlags.Public | BindingFlags.Instance);
                if (nextProp != null && i < _segments.Length - 1)
                {
                    nextValue = nextProp.GetValue(nextValue);
                }
            }
        }

        // Notify that the final value has changed
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private static object? ConvertToType(object? value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        try
        {
            return System.Convert.ChangeType(value, targetType);
        }
        catch
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unbind();
            _disposed = true;
        }
    }

    /// <summary>
    /// Path node: listens to PropertyChanged for a specific property on an object.
    /// </summary>
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
            // null PropertyName means all properties changed, or exact match
            if (e.PropertyName == null || e.PropertyName == _propertyName)
            {
                PropertyChanged?.Invoke(this, _segmentIndex);
            }
        }

        public void Unsubscribe()
        {
            if (_observable != null)
            {
                _observable.PropertyChanged -= OnPropertyChanged;
                _observable = null;
            }
            Source = null;
            PropertyChanged = null;
        }
    }
}
