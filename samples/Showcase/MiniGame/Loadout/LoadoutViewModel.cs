using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.MiniGame.Loadout;

/// <summary>
/// Shared loadout ViewModel: bag, equipment, and stats panels External-bind this instance.
/// </summary>
public partial class LoadoutViewModel : ViewModelBase
{
    private readonly IInventoryService _inventory;

    public LoadoutViewModel(IInventoryService inventory)
    {
        _inventory = inventory;
        _inventory.EnsureSeeded(200);
        _inventory.Changed += OnInventoryChanged;
        RefreshFromInventory();
    }

    public ObservableCollection<LoadoutItem> Items => _inventory.Items;

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _hintText = "";

    [ObservableProperty] private string _selectedName = "None selected";
    [ObservableProperty] private string _selectedCategory = "-";
    [ObservableProperty] private bool _canEquip;
    [ObservableProperty] private bool _canAdjust;
    [ObservableProperty] private int _selectedAttack;
    [ObservableProperty] private int _selectedDefense;
    [ObservableProperty] private int _selectedMaxHpBonus;
    [ObservableProperty] private int _selectedEnergyBonus;
    [ObservableProperty] private int _selectedPower;

    [ObservableProperty] private string _weaponText = "Empty";
    [ObservableProperty] private string _armorText = "Empty";
    [ObservableProperty] private string _accessoryText = "Empty";
    [ObservableProperty] private bool _hasWeapon;
    [ObservableProperty] private bool _hasArmor;
    [ObservableProperty] private bool _hasAccessory;

    [ObservableProperty] private int _totalAttack;
    [ObservableProperty] private int _totalDefense;
    [ObservableProperty] private int _totalMaxHp;
    [ObservableProperty] private int _totalEnergyMax;
    [ObservableProperty] private int _totalPower;

    public InteractionRequest EnterMatchRequest { get; } = new();

    [RelayCommand]
    private void AddRandomItem()
    {
        _inventory.AddRandom();
        StatusText = $"Added — {_inventory.Items.Count} items";
    }

    /// <summary>Triggered by <see cref="LoadoutItemRowView"/> ItemCommand when the row Select button is pressed.</summary>
    [RelayCommand]
    private void SelectItem(LoadoutItem item) => _inventory.Select(item.Id);

    [RelayCommand]
    private void EquipSelected()
    {
        if (_inventory.EquipSelected())
            StatusText = $"Equipped: {_inventory.SelectedItem?.Name}";
        else
            StatusText = "Cannot equip: pick Weapon/Armor/Accessory";
    }

    [RelayCommand]
    private void UnequipWeapon() => Unequip(EquipmentSlotKind.Weapon, "Weapon");

    [RelayCommand]
    private void UnequipArmor() => Unequip(EquipmentSlotKind.Armor, "Armor");

    [RelayCommand]
    private void UnequipAccessory() => Unequip(EquipmentSlotKind.Accessory, "Accessory");

    [RelayCommand]
    private void BumpAttack() => Adjust(1, 0, 0, 0);

    [RelayCommand]
    private void DropAttack() => Adjust(-1, 0, 0, 0);

    [RelayCommand]
    private void BumpDefense() => Adjust(0, 1, 0, 0);

    [RelayCommand]
    private void DropDefense() => Adjust(0, -1, 0, 0);

    [RelayCommand]
    private void BumpHp() => Adjust(0, 0, 5, 0);

    [RelayCommand]
    private void DropHp() => Adjust(0, 0, -5, 0);

    [RelayCommand]
    private void BumpEnergy() => Adjust(0, 0, 0, 5);

    [RelayCommand]
    private void DropEnergy() => Adjust(0, 0, 0, -5);

    [RelayCommand]
    private void EnterMatch() => EnterMatchRequest.Raise();

    private void Unequip(EquipmentSlotKind slot, string label)
    {
        if (_inventory.Unequip(slot))
            StatusText = $"Unequipped {label}";
    }

    private void Adjust(int attack, int defense, int hp, int energy)
    {
        if (_inventory.AdjustSelected(attack, defense, hp, energy))
            StatusText = $"Adjusted: {_inventory.SelectedItem?.Name}";
        else
            StatusText = "Cannot adjust: select equippable item";
    }

    private void OnInventoryChanged() => RefreshFromInventory();

    private void RefreshFromInventory()
    {
        var selected = _inventory.SelectedItem;
        SelectedName = selected?.Name ?? "None selected";
        SelectedCategory = selected?.Category ?? "-";
        CanEquip = selected?.EquipSlot is not null;
        CanAdjust = CanEquip;
        SelectedAttack = selected?.Attack ?? 0;
        SelectedDefense = selected?.Defense ?? 0;
        SelectedMaxHpBonus = selected?.MaxHpBonus ?? 0;
        SelectedEnergyBonus = selected?.EnergyBonus ?? 0;
        SelectedPower = selected?.Power ?? 0;

        WeaponText = FormatSlot(_inventory.Weapon);
        ArmorText = FormatSlot(_inventory.Armor);
        AccessoryText = FormatSlot(_inventory.Accessory);
        HasWeapon = _inventory.Weapon is not null;
        HasArmor = _inventory.Armor is not null;
        HasAccessory = _inventory.Accessory is not null;

        var stats = _inventory.Stats;
        TotalAttack = stats.Attack;
        TotalDefense = stats.Defense;
        TotalMaxHp = stats.MaxHp;
        TotalEnergyMax = stats.EnergyMax;
        TotalPower = stats.PowerTotal;

        StatusText = $"Bag {_inventory.Items.Count} · Power {stats.PowerTotal}";
        HintText = "Select → Equip → Adjust — all panels sync.";
    }

    private static string FormatSlot(LoadoutItem? item)
        => item is null
            ? "Empty"
            : $"{item.Name}  ATK{item.Attack}/DEF{item.Defense}/HP+{item.MaxHpBonus}";

    protected override void OnDispose()
    {
        _inventory.Changed -= OnInventoryChanged;
        base.OnDispose();
    }
}
