using CommunityToolkit.Mvvm.Messaging;
using DotPudica.Core.Logging;
using DotPudica.Core.Messaging;
using DotPudica.Core.Runtime;
using DotPudica.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Tests;

[Collection(FrameworkStaticCollection.Name)]
public class FrameworkRuntimeTests
{
    public FrameworkRuntimeTests()
    {
        FrameworkRuntime.Reset();
    }

    [Fact]
    public void ServiceLocator_Configure_AfterReset_Succeeds()
    {
        var first = ServiceLocator.Configure(services => services.AddSingleton<Marker>());
        Assert.NotNull(ServiceLocator.Get<Marker>());

        ServiceLocator.Reset();
        first.Dispose(); // already disposed by Reset; should be safe

        var second = ServiceLocator.Configure(services => services.AddSingleton<Marker>());
        Assert.NotNull(ServiceLocator.Get<Marker>());
        Assert.NotSame(first, second);

        FrameworkRuntime.Reset();
    }

    [Fact]
    public void ServiceLocator_Configure_TwiceWithoutReset_Throws()
    {
        ServiceLocator.Configure(services => services.AddSingleton<Marker>());
        Assert.Throws<InvalidOperationException>(() =>
            ServiceLocator.Configure(services => services.AddSingleton<Marker>()));
        FrameworkRuntime.Reset();
    }

    [Fact]
    public void LogManager_Reset_RestoresDefaultFactory()
    {
        LogManager.Initialize(new CountingLogFactory());
        Assert.IsType<CountingLog>(LogManager.GetLogger("x"));

        LogManager.Reset();
        var logger = LogManager.GetLogger("x");
        Assert.False(logger is CountingLog);
    }

    [Fact]
    public void MessageBus_Reset_ClearsStrongRegistrations()
    {
        var recipient = new object();
        var received = 0;
        StrongReferenceMessenger.Default.Register<TestMessage>(recipient, (_, message) =>
        {
            _ = message;
            received++;
        });
        StrongReferenceMessenger.Default.Send(new TestMessage());
        Assert.Equal(1, received);

        MessageBus.Reset();
        StrongReferenceMessenger.Default.Send(new TestMessage());
        Assert.Equal(1, received);
    }

    [Fact]
    public void FrameworkRuntime_Reset_ClearsServiceLocator()
    {
        ServiceLocator.Configure(services => services.AddSingleton<Marker>());
        FrameworkRuntime.Reset();
        Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<Marker>());
    }

    private sealed class Marker;

    private sealed class TestMessage;

    private sealed class CountingLogFactory : ILogFactory
    {
        public ILog GetLogger(Type type) => new CountingLog();
        public ILog GetLogger(string name) => new CountingLog();
    }

    private sealed class CountingLog : ILog
    {
        public bool IsDebugEnabled => false;
        public bool IsInfoEnabled => false;
        public bool IsWarnEnabled => false;
        public bool IsErrorEnabled => false;
        public void Debug(string message) { }
        public void Debug(string format, params object[] args) { }
        public void Info(string message) { }
        public void Info(string format, params object[] args) { }
        public void Warn(string message) { }
        public void Warn(string format, params object[] args) { }
        public void Error(string message) { }
        public void Error(string message, Exception exception) { }
        public void Error(string format, params object[] args) { }
        public void Fatal(string message) { }
        public void Fatal(string message, Exception exception) { }
    }
}
