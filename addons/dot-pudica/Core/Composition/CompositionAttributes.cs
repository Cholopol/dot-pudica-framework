using System;

namespace DotPudica.Core.Composition;

/// <summary>
/// Marks a View field or property for service injection from the application context.
/// The source generator assigns the resolved service in the generated lifecycle, before any user hook runs.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class InjectAttribute : Attribute
{
}

/// <summary>
/// Marks a View method as the ViewModel factory. Used when the ViewModel constructor cannot be
/// fully resolved from DI (non-interface parameters, multiple constructors, or custom construction).
/// The method must be parameterless and return the ViewModel type declared by [DotPudicaView].
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ViewModelFactoryAttribute : Attribute
{
}

/// <summary>
/// Marks a View method as an event handler for a ViewModel event.
/// The source generator subscribes after bindings are initialized and unsubscribes during teardown,
/// eliminating the most common leak-prone boilerplate. Repeatable: one event per attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SubscribeAttribute : Attribute
{
    public string EventPath { get; }

    public SubscribeAttribute(string eventPath)
    {
        EventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
    }
}
