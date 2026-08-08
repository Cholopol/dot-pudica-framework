namespace DotPudica.Tests;

/// <summary>
/// Serializes tests that mutate DotPudica Core static singletons (ServiceLocator, messengers, LogManager).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FrameworkStaticCollection : ICollectionFixture<object>
{
    public const string Name = "FrameworkStatic";
}
