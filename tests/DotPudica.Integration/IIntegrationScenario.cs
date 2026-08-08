using Godot;

namespace DotPudica.Integration;

/// <summary>Integration scenario that can be executed sequentially in IntegrationTestRunner.</summary>
public interface IIntegrationScenario
{
    string Name { get; }

    Task<IntegrationResult> RunAsync(Node host);
}
