namespace DotPudica.Core.Binding;

/// <summary>Typed target property proxy; avoids boxing on the value path.</summary>
public interface ITypedTargetProxy<TValue> : IDisposable
{
    TValue GetValue();
    void SetValue(TValue value);

    /// <summary>Raised when the control value is modified by the user (TwoWay).</summary>
    event EventHandler? ValueChanged;
}

/// <summary>Type-erased target proxy for the non-generic PropertyBinding pipeline and test stubs.</summary>
public interface ITargetProxy : ITypedTargetProxy<object?>
{
}
