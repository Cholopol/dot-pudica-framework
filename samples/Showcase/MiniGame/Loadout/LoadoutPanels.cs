using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.MiniGame.Loadout;

/// <summary>Bag panel: virtual list. Shares LoadoutViewModel with equipment and stats panels.</summary>
[DotPudicaView(typeof(LoadoutViewModel), AutoInitialize = false)]
public partial class LoadoutBagPanel : VBoxContainer
{
    private const string ItemScenePath = "res://samples/Showcase/MiniGame/Loadout/LoadoutItemRow.tscn";

    [Export, BindTo(nameof(LoadoutViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.AddRandomItemCommand))]
    private Button _addItemButton = null!;

    [Export, ItemsSource(nameof(LoadoutViewModel.Items), ItemScenePath, ItemCommand = nameof(LoadoutViewModel.SelectItemCommand))]
    private LoadoutItemsControl _itemList = null!;

    public LoadoutBagPanel()
    {
        Name = "BagPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 8);

        ShowcaseUi.AddSection(this, "Bag");

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        AddChild(_statusLabel);

        _itemList = new LoadoutItemsControl
        {
            Name = "ItemList",
            ItemHeight = 40,
            Overscan = 2,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        AddChild(_itemList);

        _addItemButton = ShowcaseUi.CreatePrimaryButton("Add Random Item");
        AddChild(_addItemButton);
    }

    public override void _ExitTree() => DisposeView();

    public void BindShared(LoadoutViewModel shared)
    {
        SetViewModel(shared, ViewModelOwnership.External);
        DotPudicaInitialize();
    }
}

/// <summary>Equipment panel: three slots + unequip. Shares LoadoutViewModel.</summary>
[DotPudicaView(typeof(LoadoutViewModel), AutoInitialize = false)]
public partial class LoadoutEquipmentPanel : VBoxContainer
{
    [Export, BindTo(nameof(LoadoutViewModel.WeaponText))]
    private Label _weaponLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.ArmorText))]
    private Label _armorLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.AccessoryText))]
    private Label _accessoryLabel = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.UnequipWeaponCommand))]
    private Button _unequipWeaponButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.UnequipArmorCommand))]
    private Button _unequipArmorButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.UnequipAccessoryCommand))]
    private Button _unequipAccessoryButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.EquipSelectedCommand))]
    private Button _equipButton = null!;

    public LoadoutEquipmentPanel()
    {
        Name = "EquipmentPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 8);

        ShowcaseUi.AddSection(this, "Equipment");

        _weaponLabel = SlotRow(this, "Weapon", out _unequipWeaponButton);
        _armorLabel = SlotRow(this, "Armor", out _unequipArmorButton);
        _accessoryLabel = SlotRow(this, "Accessory", out _unequipAccessoryButton);

        _equipButton = ShowcaseUi.CreatePrimaryButton("Equip Selected");
        AddChild(_equipButton);
    }

    public override void _ExitTree() => DisposeView();

    public void BindShared(LoadoutViewModel shared)
    {
        SetViewModel(shared, ViewModelOwnership.External);
        DotPudicaInitialize();
    }

    private static Label SlotRow(VBoxContainer parent, string title, out Button unequip)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        row.AddChild(new Label
        {
            Text = title,
            CustomMinimumSize = new Vector2(72, 0),
            Modulate = ShowcaseTheme.Muted
        });
        var label = new Label
        {
            Text = "Empty",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        row.AddChild(label);

        unequip = ShowcaseUi.CreateActionButton("Unequip");
        row.AddChild(unequip);
        return label;
    }
}

/// <summary>Stats panel: edit selected item values + deploy totals. Shares LoadoutViewModel.</summary>
[DotPudicaView(typeof(LoadoutViewModel), AutoInitialize = false)]
public partial class LoadoutStatsPanel : VBoxContainer
{
    [Export, BindTo(nameof(LoadoutViewModel.HintText))]
    private Label _hintLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedName))]
    private Label _selectedNameLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedCategory))]
    private Label _selectedCategoryLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedAttack), Converter = typeof(IntToStringConverter))]
    private Label _attackValueLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedDefense), Converter = typeof(IntToStringConverter))]
    private Label _defenseValueLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedMaxHpBonus), Converter = typeof(IntToStringConverter))]
    private Label _hpValueLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedEnergyBonus), Converter = typeof(IntToStringConverter))]
    private Label _energyValueLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.SelectedPower), Converter = typeof(IntToStringConverter))]
    private Label _powerValueLabel = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.BumpAttackCommand))]
    private Button _bumpAttackButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.DropAttackCommand))]
    private Button _dropAttackButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.BumpDefenseCommand))]
    private Button _bumpDefenseButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.DropDefenseCommand))]
    private Button _dropDefenseButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.BumpHpCommand))]
    private Button _bumpHpButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.DropHpCommand))]
    private Button _dropHpButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.BumpEnergyCommand))]
    private Button _bumpEnergyButton = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.DropEnergyCommand))]
    private Button _dropEnergyButton = null!;

    [Export, BindTo(nameof(LoadoutViewModel.TotalAttack), Converter = typeof(IntToStringConverter))]
    private Label _totalAttackLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.TotalDefense), Converter = typeof(IntToStringConverter))]
    private Label _totalDefenseLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.TotalMaxHp), Converter = typeof(IntToStringConverter))]
    private Label _totalMaxHpLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.TotalEnergyMax), Converter = typeof(IntToStringConverter))]
    private Label _totalEnergyLabel = null!;

    [Export, BindTo(nameof(LoadoutViewModel.TotalPower), Converter = typeof(IntToStringConverter))]
    private Label _totalPowerLabel = null!;

    public LoadoutStatsPanel()
    {
        Name = "StatsPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 8);

        ShowcaseUi.AddSection(this, "Stats");

        _hintLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        AddChild(_hintLabel);

        _selectedNameLabel = new Label();
        AddChild(_selectedNameLabel);
        _selectedCategoryLabel = new Label { Modulate = ShowcaseTheme.Muted };
        AddChild(_selectedCategoryLabel);

        StatEditor(this, "ATK", out _attackValueLabel, out _dropAttackButton, out _bumpAttackButton);
        StatEditor(this, "DEF", out _defenseValueLabel, out _dropDefenseButton, out _bumpDefenseButton);
        StatEditor(this, "HP+", out _hpValueLabel, out _dropHpButton, out _bumpHpButton);
        StatEditor(this, "EN+", out _energyValueLabel, out _dropEnergyButton, out _bumpEnergyButton);

        var powerRow = new HBoxContainer();
        AddChild(powerRow);
        powerRow.AddChild(new Label { Text = "Power", Modulate = ShowcaseTheme.Muted });
        _powerValueLabel = new Label { Text = "0" };
        powerRow.AddChild(_powerValueLabel);

        var totals = ShowcaseUi.AddMetricsRow(this);
        ShowcaseUi.AddMetricChip(totals, "Attack", out _totalAttackLabel);
        ShowcaseUi.AddMetricChip(totals, "Defense", out _totalDefenseLabel);
        ShowcaseUi.AddMetricChip(totals, "Max HP", out _totalMaxHpLabel);
        ShowcaseUi.AddMetricChip(totals, "Energy", out _totalEnergyLabel);
        ShowcaseUi.AddMetricChip(totals, "Power", out _totalPowerLabel);
    }

    public override void _ExitTree() => DisposeView();

    public void BindShared(LoadoutViewModel shared)
    {
        SetViewModel(shared, ViewModelOwnership.External);
        DotPudicaInitialize();
    }

    private static void StatEditor(
        VBoxContainer parent,
        string title,
        out Label valueLabel,
        out Button drop,
        out Button bump)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        parent.AddChild(row);

        row.AddChild(new Label
        {
            Text = title,
            CustomMinimumSize = new Vector2(40, 0),
            Modulate = ShowcaseTheme.Muted
        });
        drop = ShowcaseUi.CreateActionButton("-");
        row.AddChild(drop);
        valueLabel = new Label
        {
            Text = "0",
            CustomMinimumSize = new Vector2(36, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        row.AddChild(valueLabel);
        bump = ShowcaseUi.CreateActionButton("+");
        row.AddChild(bump);
    }
}
