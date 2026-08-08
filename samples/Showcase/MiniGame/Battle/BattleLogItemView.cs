using DotPudica.Godot.Binding.ControlProxies;
using Godot;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Battle;

/// <summary>Battle log entry template.</summary>
public partial class BattleLogItemView : PanelContainer, IItemsControlItem
{
    private Label? _textLabel;

    public override void _Ready() => _textLabel = GetNodeOrNull<Label>("TextLabel");

    public object? DataContext
    {
        get => _textLabel?.Text;
        set
        {
            if (_textLabel is null)
                return;

            _textLabel.Text = value is BattleLogEntry entry
                ? $"[{entry.Tick}] {entry.Text}"
                : value?.ToString() ?? "";
        }
    }
}
