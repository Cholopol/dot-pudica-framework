using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace Samples.Showcase.Gallery.VirtualList;

/// <summary>Virtual list item template root node, reused by VirtualizedItemsControl's node pool.</summary>
public partial class VirtualListItemView : PanelContainer, IItemsControlItem
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

            if (value is VirtualListItemModel item)
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
