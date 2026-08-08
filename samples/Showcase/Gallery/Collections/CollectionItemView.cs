using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace Samples.Showcase.Gallery.Collections;

/// <summary>Collection list item template root node. Reused by both PoolSize=0 and PoolSize=32 lists.</summary>
public partial class CollectionItemView : PanelContainer, IItemsControlItem
{
    private Label? _idLabel;
    private Label? _titleLabel;

    public override void _Ready()
    {
        _idLabel = GetNodeOrNull<Label>("HBox/IdLabel");
        _titleLabel = GetNodeOrNull<Label>("HBox/TitleLabel");
    }

    public object? DataContext
    {
        get => _titleLabel?.Text;
        set
        {
            if (_idLabel is null || _titleLabel is null)
                return;

            if (value is CollectionItemModel item)
            {
                _idLabel.Text = $"#{item.Id}";
                _titleLabel.Text = item.Title;
            }
            else
            {
                _idLabel.Text = "";
                _titleLabel.Text = value?.ToString() ?? "";
            }
        }
    }
}
