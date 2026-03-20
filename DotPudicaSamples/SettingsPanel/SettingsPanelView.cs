using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.SettingsPanel;

[DotPudicaView(typeof(SettingsPanelViewModel))]
public partial class SettingsPanelView : Control
{
    // Slider two-way bound to volume (double type)
    [Export, BindTo("MasterVolume", Mode = BindingMode.TwoWay)]
    private HSlider _volumeSlider = null!;

    // Label one-way displays volume text (provided by ViewModel's VolumeText property)
    [Export, BindTo("VolumeText", Mode = BindingMode.OneWay)]
    private Label _volumeLabel = null!;

    // CheckBox two-way bound to boolean
    [Export, BindTo("IsMusicEnabled", Mode = BindingMode.TwoWay)]
    private CheckBox _musicToggle = null!;

    // OptionButton two-way bound to selected index (int type)
    [Export, BindTo("QualityLevel", Mode = BindingMode.TwoWay)]
    private OptionButton _qualityOption = null!;

    // Button command binding
    [Export, BindCommand("SaveCommand")]
    private Button _saveButton = null!;

    public override void _Ready()
    {
        // Pre-populate OptionButton options (can also be added directly in Godot editor)
        _qualityOption.AddItem("Low");
        _qualityOption.AddItem("Medium");
        _qualityOption.AddItem("High");
        _qualityOption.AddItem("Ultra");

        ViewModel = new SettingsPanelViewModel();
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        DotPudicaDispose();
        base._ExitTree();
    }
}
