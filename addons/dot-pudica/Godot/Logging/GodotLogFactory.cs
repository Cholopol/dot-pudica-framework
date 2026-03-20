using DotPudica.Core.Logging;
using Godot;

namespace DotPudica.Godot.Logging;

/// <summary>
/// Godot GD.Print-based log factory. Inject into LogManager at application startup:
/// <code>
/// LogManager.Initialize(new GodotLogFactory());
/// </code>
/// </summary>
public class GodotLogFactory : ILogFactory
{
    public ILog GetLogger(Type type) => new GodotLog(type.Name);
    public ILog GetLogger(string name) => new GodotLog(name);
}

/// <summary>
/// Godot GD.Print-based log implementation.
/// </summary>
public class GodotLog : ILog
{
    private readonly string _name;

    public GodotLog(string name) => _name = name;

    public bool IsDebugEnabled => OS.IsDebugBuild();
    public bool IsInfoEnabled => true;
    public bool IsWarnEnabled => true;
    public bool IsErrorEnabled => true;

    private string Format(string level, string msg)
        => $"[{level}][{_name}] {msg}";

    public void Debug(string message)
    {
        if (IsDebugEnabled)
            GD.Print(Format("DEBUG", message));
    }

    public void Debug(string format, params object[] args)
        => Debug(string.Format(format, args));

    public void Info(string message)
        => GD.Print(Format("INFO", message));

    public void Info(string format, params object[] args)
        => Info(string.Format(format, args));

    public void Warn(string message)
        => GD.PushWarning(Format("WARN", message));

    public void Warn(string format, params object[] args)
        => Warn(string.Format(format, args));

    public void Error(string message)
        => GD.PushError(Format("ERROR", message));

    public void Error(string message, Exception exception)
        => GD.PushError(Format("ERROR", $"{message}\n{exception}"));

    public void Error(string format, params object[] args)
        => Error(string.Format(format, args));

    public void Fatal(string message)
        => GD.PushError(Format("FATAL", message));

    public void Fatal(string message, Exception exception)
        => GD.PushError(Format("FATAL", $"{message}\n{exception}"));
}
