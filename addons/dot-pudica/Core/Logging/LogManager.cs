namespace DotPudica.Core.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// Lightweight log abstraction without a Microsoft.Extensions.Logging dependency,
/// so hosts can swap backends (Godot, console, etc.).
/// </summary>
public interface ILog
{
    bool IsDebugEnabled { get; }
    bool IsInfoEnabled { get; }
    bool IsWarnEnabled { get; }
    bool IsErrorEnabled { get; }

    void Debug(string message);
    void Debug(string format, params object[] args);
    void Info(string message);
    void Info(string format, params object[] args);
    void Warn(string message);
    void Warn(string format, params object[] args);
    void Error(string message);
    void Error(string message, Exception exception);
    void Error(string format, params object[] args);
    void Fatal(string message);
    void Fatal(string message, Exception exception);
}

public interface ILogFactory
{
    ILog GetLogger(Type type);
    ILog GetLogger(string name);
}

public static class LogManager
{
    private static ILogFactory _factory = new DefaultLogFactory();

    public static void Initialize(ILogFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Restore the default console factory so host-specific factories do not pin the ALC.
    /// </summary>
    public static void Reset()
    {
        _factory = new DefaultLogFactory();
    }

    public static ILog GetLogger<T>() => _factory.GetLogger(typeof(T));
    public static ILog GetLogger(Type type) => _factory.GetLogger(type);
    public static ILog GetLogger(string name) => _factory.GetLogger(name);
}

internal sealed class DefaultLogFactory : ILogFactory
{
    public ILog GetLogger(Type type) => new ConsoleLog(type.Name);
    public ILog GetLogger(string name) => new ConsoleLog(name);
}

internal sealed class ConsoleLog : ILog
{
    private readonly string _name;

    public ConsoleLog(string name) => _name = name;

    public bool IsDebugEnabled => true;
    public bool IsInfoEnabled => true;
    public bool IsWarnEnabled => true;
    public bool IsErrorEnabled => true;

    private void Write(string level, string msg)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][{level}][{_name}] {msg}");

    public void Debug(string message) => Write("DEBUG", message);
    public void Debug(string format, params object[] args) => Write("DEBUG", string.Format(format, args));
    public void Info(string message) => Write("INFO", message);
    public void Info(string format, params object[] args) => Write("INFO", string.Format(format, args));
    public void Warn(string message) => Write("WARN", message);
    public void Warn(string format, params object[] args) => Write("WARN", string.Format(format, args));
    public void Error(string message) => Write("ERROR", message);
    public void Error(string message, Exception ex) => Write("ERROR", $"{message}\n{ex}");
    public void Error(string format, params object[] args) => Write("ERROR", string.Format(format, args));
    public void Fatal(string message) => Write("FATAL", message);
    public void Fatal(string message, Exception ex) => Write("FATAL", $"{message}\n{ex}");
}
