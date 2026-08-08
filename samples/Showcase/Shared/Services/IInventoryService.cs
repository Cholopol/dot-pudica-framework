using System.Collections.ObjectModel;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.Shared.Services;

/// <summary>
/// Shared inventory + equipment slots. Collection and equipment state can only be modified on the UI thread.
/// </summary>
public interface IInventoryService
{
    ObservableCollection<LoadoutItem> Items { get; }

    LoadoutItem? Weapon { get; }
    LoadoutItem? Armor { get; }
    LoadoutItem? Accessory { get; }
    string? SelectedItemId { get; }
    LoadoutItem? SelectedItem { get; }
    PlayerCombatStats Stats { get; }

    event Action? Changed;

    void EnsureSeeded(int count = 200);
    void AddRandom();
    bool RemoveAt(int index);
    void Select(string? itemId);
    bool EquipSelected();
    bool Unequip(EquipmentSlotKind slot);
    bool AdjustSelected(int attackDelta, int defenseDelta, int hpDelta, int energyDelta);
}

public sealed class FakeInventoryService : IInventoryService
{
    private static readonly string[] Categories = ["Weapon", "Armor", "Consumable", "Accessory"];
    private const int BaseAttack = 10;
    private const int BaseDefense = 5;
    private const int BaseMaxHp = 100;
    private const int BaseEnergyMax = 100;

    private int _nextId = 1;
    private string? _weaponId;
    private string? _armorId;
    private string? _accessoryId;

    public ObservableCollection<LoadoutItem> Items { get; } = new();

    public LoadoutItem? Weapon => FindById(_weaponId);
    public LoadoutItem? Armor => FindById(_armorId);
    public LoadoutItem? Accessory => FindById(_accessoryId);
    public string? SelectedItemId { get; private set; }
    public LoadoutItem? SelectedItem => FindById(SelectedItemId);

    public PlayerCombatStats Stats
    {
        get
        {
            var attack = BaseAttack + Sum(static i => i.Attack);
            var defense = BaseDefense + Sum(static i => i.Defense);
            var maxHp = BaseMaxHp + Sum(static i => i.MaxHpBonus);
            var energy = BaseEnergyMax + Sum(static i => i.EnergyBonus);
            var power = attack + defense + maxHp / 2 + energy / 4;
            return new PlayerCombatStats(attack, defense, maxHp, energy, power);
        }
    }

    public event Action? Changed;

    public void EnsureSeeded(int count = 200)
    {
        if (Items.Count > 0)
            return;

        for (var i = 0; i < count; i++)
            Items.Add(CreateItem());

        RaiseChanged();
    }

    public void AddRandom()
    {
        Items.Add(CreateItem());
        RaiseChanged();
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= Items.Count)
            return false;

        var removed = Items[index];
        Items.RemoveAt(index);
        ClearSlotIfMatches(removed.Id);
        if (SelectedItemId == removed.Id)
            SelectedItemId = null;

        RaiseChanged();
        return true;
    }

    public void Select(string? itemId)
    {
        if (SelectedItemId == itemId)
            return;

        SelectedItemId = itemId is null || FindById(itemId) is null ? null : itemId;
        RaiseChanged();
    }

    public bool EquipSelected()
    {
        var item = SelectedItem;
        if (item?.EquipSlot is not { } slot)
            return false;

        switch (slot)
        {
            case EquipmentSlotKind.Weapon:
                _weaponId = item.Id;
                break;
            case EquipmentSlotKind.Armor:
                _armorId = item.Id;
                break;
            case EquipmentSlotKind.Accessory:
                _accessoryId = item.Id;
                break;
            default:
                return false;
        }

        RaiseChanged();
        return true;
    }

    public bool Unequip(EquipmentSlotKind slot)
    {
        var changed = false;
        switch (slot)
        {
            case EquipmentSlotKind.Weapon:
                if (_weaponId is not null) { _weaponId = null; changed = true; }
                break;
            case EquipmentSlotKind.Armor:
                if (_armorId is not null) { _armorId = null; changed = true; }
                break;
            case EquipmentSlotKind.Accessory:
                if (_accessoryId is not null) { _accessoryId = null; changed = true; }
                break;
        }

        if (changed)
            RaiseChanged();
        return changed;
    }

    public bool AdjustSelected(int attackDelta, int defenseDelta, int hpDelta, int energyDelta)
    {
        var item = SelectedItem;
        if (item is null || item.EquipSlot is null)
            return false;

        var index = IndexOfId(item.Id);
        if (index < 0)
            return false;

        var attack = Math.Max(0, item.Attack + attackDelta);
        var defense = Math.Max(0, item.Defense + defenseDelta);
        var hp = Math.Max(0, item.MaxHpBonus + hpDelta);
        var energy = Math.Max(0, item.EnergyBonus + energyDelta);
        var power = attack + defense + hp / 2 + energy / 4;
        var updated = item with
        {
            Attack = attack,
            Defense = defense,
            MaxHpBonus = hp,
            EnergyBonus = energy,
            Power = power
        };

        Items[index] = updated;
        RaiseChanged();
        return true;
    }

    private int Sum(Func<LoadoutItem, int> selector)
    {
        var total = 0;
        if (Weapon is { } w) total += selector(w);
        if (Armor is { } a) total += selector(a);
        if (Accessory is { } x) total += selector(x);
        return total;
    }

    private LoadoutItem? FindById(string? id)
    {
        if (id is null)
            return null;
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == id)
                return Items[i];
        }
        return null;
    }

    private int IndexOfId(string id)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == id)
                return i;
        }
        return -1;
    }

    private void ClearSlotIfMatches(string id)
    {
        if (_weaponId == id) _weaponId = null;
        if (_armorId == id) _armorId = null;
        if (_accessoryId == id) _accessoryId = null;
    }

    private void RaiseChanged() => Changed?.Invoke();

    private LoadoutItem CreateItem()
    {
        var id = _nextId++;
        var cat = Categories[id % Categories.Length];
        var roll = 10 + id % 90;

        return cat switch
        {
            "Weapon" => Make(id, cat, EquipmentSlotKind.Weapon, attack: roll, defense: 0, hp: 0, energy: roll / 4),
            "Armor" => Make(id, cat, EquipmentSlotKind.Armor, attack: 0, defense: roll, hp: roll, energy: 0),
            "Accessory" => Make(id, cat, EquipmentSlotKind.Accessory, attack: roll / 3, defense: roll / 3, hp: roll / 2, energy: roll / 2),
            _ => Make(id, cat, null, attack: 0, defense: 0, hp: 5 + roll / 10, energy: 10 + roll / 5)
        };
    }

    private static LoadoutItem Make(
        int id,
        string category,
        EquipmentSlotKind? slot,
        int attack,
        int defense,
        int hp,
        int energy)
    {
        var power = attack + defense + hp / 2 + energy / 4;
        return new LoadoutItem(
            $"item-{id}",
            $"{category}#{id}",
            category,
            power,
            slot,
            attack,
            defense,
            hp,
            energy);
    }
}
