using System.Windows.Input;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Loadout;

/// <summary>
/// Virtual list row: displays item stats; Select button invokes the host-injected ItemCommand.
/// </summary>
public partial class LoadoutItemRowView : PanelContainer, IItemsControlItem, IItemsControlItemCommand
{
    private Label? _nameLabel;
    private Label? _categoryLabel;
    private Label? _powerLabel;
    private Button? _selectButton;
    private LoadoutItem? _item;

    public ICommand? ItemCommand { get; set; }

    public override void _Ready()
    {
        _nameLabel = GetNodeOrNull<Label>("HBox/NameLabel");
        _categoryLabel = GetNodeOrNull<Label>("HBox/CategoryLabel");
        _powerLabel = GetNodeOrNull<Label>("HBox/PowerLabel");
        _selectButton = GetNodeOrNull<Button>("HBox/SelectButton");
        if (_selectButton is not null)
        {
            _selectButton.Pressed += OnSelectPressed;
            ShowcaseUi.ApplyActionButton(_selectButton);
        }
    }

    public object? DataContext
    {
        get => _item;
        set
        {
            if (_nameLabel is null || _categoryLabel is null || _powerLabel is null)
                return;

            if (value is LoadoutItem item)
            {
                _item = item;
                _nameLabel.Text = item.Name;
                _categoryLabel.Text = item.Category;
                _powerLabel.Text = item.EquipSlot is null
                    ? $"Consumable · Power {item.Power}"
                    : $"ATK{item.Attack} DEF{item.Defense} HP+{item.MaxHpBonus}";
            }
            else
            {
                _item = null;
                _nameLabel.Text = "";
                _categoryLabel.Text = "";
                _powerLabel.Text = "";
            }
        }
    }

    private void OnSelectPressed()
    {
        if (_item is null)
            return;
        if (ItemCommand?.CanExecute(_item) == true)
            ItemCommand.Execute(_item);
    }
}
