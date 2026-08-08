namespace DotPudica.Integration;

/// <summary>Single scenario result for Godot headless integration tests.</summary>
public sealed record IntegrationResult(string Name, bool Passed, string? FailureReason = null)
{
    public static IntegrationResult Pass(string name) => new(name, true);

    public static IntegrationResult Fail(string name, string reason) => new(name, false, reason);
}
