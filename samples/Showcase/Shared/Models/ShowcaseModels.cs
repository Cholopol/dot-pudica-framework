namespace Samples.Showcase.Shared.Models;

/// <summary>Lobby room list snapshot (immutable), published by background heartbeat.</summary>
public sealed record RoomInfo(string Id, string Name, int PlayerCount, int MaxPlayers);

public sealed record RoomSnapshot(IReadOnlyList<RoomInfo> Rooms, long Sequence);

/// <summary>Equipment slot type. Consumables are null, cannot be equipped.</summary>
public enum EquipmentSlotKind
{
    Weapon,
    Armor,
    Accessory
}

/// <summary>Loadout inventory item. Power is derived from attack/defense etc., can be adjusted and written back on the loadout page.</summary>
public sealed record LoadoutItem(
    string Id,
    string Name,
    string Category,
    int Power,
    EquipmentSlotKind? EquipSlot,
    int Attack,
    int Defense,
    int MaxHpBonus,
    int EnergyBonus);

/// <summary>Combat stats snapshot aggregated from current equipment.</summary>
public sealed record PlayerCombatStats(
    int Attack,
    int Defense,
    int MaxHp,
    int EnergyMax,
    int PowerTotal);

/// <summary>Battle HUD per-tick snapshot (value-type friendly fields).</summary>
public sealed record BattleTick(int Hp, int MaxHp, float Energy, int Score, long Tick);

/// <summary>Match result (Showcase-internal immutable DTO).</summary>
public sealed record ShowcaseMatchResult(string RoomId, int PlayerCount);

/// <summary>Battle result.</summary>
public sealed record BattleResult(
    int FinalScore,
    bool Won,
    TimeSpan Duration,
    int KillCount = 0,
    PlayerCombatStats? LoadoutStats = null);

/// <summary>Battle log entry.</summary>
public sealed record BattleLogEntry(string Text, long Tick);
