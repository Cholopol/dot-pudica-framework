using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.HUD;

public partial class HudViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HealthPercent), nameof(HealthText))]
    private double _health = 85;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HealthPercent), nameof(HealthText))]
    private double _maxHealth = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManaPercent), nameof(ManaText))]
    private double _mana = 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManaPercent), nameof(ManaText))]
    private double _maxMana = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GoldText))]
    private int _gold = 1234;

    // Calculated property: automatically derived from Source properties
    public double HealthPercent => MaxHealth > 0 ? Health / MaxHealth * 100 : 0;
    public string  HealthText    => $"HP: {(int)Health}/{(int)MaxHealth}";
    public double ManaPercent   => MaxMana > 0 ? Mana / MaxMana * 100 : 0;
    public string  ManaText      => $"MP: {(int)Mana}/{(int)MaxMana}";
    public string  GoldText      => $"Gold: {Gold:N0}";

    // Mock data for testing purposes
    /// <summary>Simulate taking damage</summary>
    public void TakeDamage(double amount)
    {
        Health = Math.Max(0, Health - amount);
        if (Health <= 0)
            Send(new DotPudica.Core.Messaging.NotificationMessage("PlayerDead"));
    }

    /// <summary>Simulate consuming mana</summary>
    public void ConsumeMana(double amount)
        => Mana = Math.Max(0, Mana - amount);

    /// <summary>Simulate picking up gold</summary>
    public void PickupGold(int amount)
        => Gold += amount;
}
