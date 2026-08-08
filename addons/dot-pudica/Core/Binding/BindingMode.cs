namespace DotPudica.Core.Binding;

public enum BindingMode
{
    /// <summary>
    /// Resolved at compile time by the generator (input → TwoWay, display → OneWay);
    /// outside the generator behaves as OneWay.
    /// </summary>
    Default = 0,

    /// <summary>ViewModel → View.</summary>
    OneWay,

    /// <summary>ViewModel ↔ View.</summary>
    TwoWay,

    /// <summary>Synchronizes once at bind time only.</summary>
    OneTime,

    /// <summary>View → ViewModel.</summary>
    OneWayToSource
}
