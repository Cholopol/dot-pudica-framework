using DotPudica.Core.Binding;
using Godot;

namespace DotPudica.Integration.Controls;

/// <summary>Pure CLR access counter, used for assertions after ExitTree (does not depend on disposed Godot objects).</summary>
public sealed class TargetAccessLog
{
    public int AccessCount { get; private set; }
    public List<int> SetThreadIds { get; } = new();

    public void RecordAccess()
    {
        AccessCount++;
        SetThreadIds.Add(System.Environment.CurrentManagedThreadId);
    }
}

/// <summary>Wraps a real LabelProxy, recording each SetValue/GetValue.</summary>
public sealed class LoggingTargetProxy : ITargetProxy
{
    private readonly ITargetProxy _inner;
    private readonly TargetAccessLog _log;

    public LoggingTargetProxy(ITargetProxy inner, TargetAccessLog log)
    {
        _inner = inner;
        _log = log;
    }

    public event EventHandler? ValueChanged
    {
        add => _inner.ValueChanged += value;
        remove => _inner.ValueChanged -= value;
    }

    public object? GetValue()
    {
        _log.RecordAccess();
        return _inner.GetValue();
    }

    public void SetValue(object? value)
    {
        _log.RecordAccess();
        _inner.SetValue(value);
    }

    public void Dispose() => _inner.Dispose();
}

/// <summary>Disposes BindingContext in _ExitTree, simulating DotPudicaDispose of a real View.</summary>
public partial class BindingHostControl : Control
{
    private BindingContext? _context;

    public void Attach(BindingContext context) => _context = context;

    public override void _ExitTree()
    {
        _context?.Dispose();
        _context = null;
        base._ExitTree();
    }
}
