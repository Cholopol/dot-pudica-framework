using DotPudica.Core.Binding;

namespace Samples.Showcase.Shared.Probes;

/// <summary>
/// String target proxy independent of Godot controls: forwards each write to a callback for probe recording.
/// <see cref="RaiseValueChanged"/> is used to simulate TwoWay target-side changes (Probe G).
/// </summary>
public sealed class RecordingStringProxy : ITypedTargetProxy<string>
{
    private readonly Action<string>? _onWrite;
    private string _value = "";

    public RecordingStringProxy(Action<string>? onWrite = null) => _onWrite = onWrite;

    public event EventHandler? ValueChanged;

    public string GetValue() => _value;

    public void SetValue(string value)
    {
        _value = value;
        _onWrite?.Invoke(value);
    }

    public void RaiseValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose() => ValueChanged = null;
}
