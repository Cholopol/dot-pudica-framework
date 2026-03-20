namespace DotPudica.Core.Logging;

/// <summary>
/// Log level.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// Framework log interface. Corresponds to Loxodon's ILog.
/// Uses lightweight abstraction rather than strong dependency on Microsoft.Extensions.Logging,
/// convenient for switching log backends in different environments like Godot, console, etc.
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

/// <summary>
/// Log factory interface. Corresponds to Loxodon's ILogFactory.
/// </summary>
public interface ILogFactory
{
    ILog GetLogger(Type type);
    ILog GetLogger(string name);
}

/// <summary>
/// Log manager. Corresponds to Loxodon's LogManager, serves as global access point.
/// </summary>
public static class LogManager
{
    private static ILogFactory _factory = new DefaultLogFactory();

    /// <summary>
    /// Replace log factory (configure once at startup, e.g., switch to Godot GD.Print backend).
    /// </summary>
    public static void Initialize(ILogFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public static ILog GetLogger<T>() => _factory.GetLogger(typeof(T));
    public static ILog GetLogger(Type type) => _factory.GetLogger(type);
    public static ILog GetLogger(string name) => _factory.GetLogger(name);
}

/// <summary>
/// Default log factory (writes to Console). Can be replaced with GodotLogFactory in Godot environment.
/// </summary>
internal sealed class DefaultLogFactory : ILogFactory
{
    public ILog GetLogger(Type type) => new ConsoleLog(type.Name);
    public ILog GetLogger(string name) => new ConsoleLog(name);
}

/// <summary>
/// Simple log implementation based on Console.WriteLine (suitable for unit testing or non-Godot environments).
/// </summary>
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
