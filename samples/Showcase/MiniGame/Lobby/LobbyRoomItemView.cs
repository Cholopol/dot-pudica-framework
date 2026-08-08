using DotPudica.Godot.Binding.ControlProxies;
using Godot;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Lobby;

/// <summary>Lobby room row for ItemsSource pooling.</summary>
public partial class LobbyRoomItemView : PanelContainer, IItemsControlItem
{
    private Label? _nameLabel;
    private Label? _playersLabel;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", ShowcaseTheme.PanelStyle());
        _nameLabel = GetNodeOrNull<Label>("HBox/NameLabel");
        _playersLabel = GetNodeOrNull<Label>("HBox/PlayersLabel");
    }

    public object? DataContext
    {
        get => _nameLabel?.Text;
        set
        {
            if (_nameLabel is null || _playersLabel is null)
                return;

            if (value is RoomInfo room)
            {
                _nameLabel.Text = room.Name;
                _playersLabel.Text = $"{room.PlayerCount}/{room.MaxPlayers} players";
            }
            else
            {
                _nameLabel.Text = "";
                _playersLabel.Text = "";
            }
        }
    }
}
