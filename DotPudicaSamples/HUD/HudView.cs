using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.HUD;

[DotPudicaView(typeof(HudViewModel))]
public partial class HudView : Control
{
	// ProgressBar bound to percentage value (0~100)
	[Export, BindTo("HealthPercent", Mode = BindingMode.OneWay)]
	private ProgressBar _healthBar = null!;

	// Label bound to formatted text (e.g. "HP: 85/100")
	[Export, BindTo("HealthText", Mode = BindingMode.OneWay)]
	private Label _healthLabel = null!;

	[Export, BindTo("ManaPercent", Mode = BindingMode.OneWay)]
	private ProgressBar _manaBar = null!;

	[Export, BindTo("ManaText", Mode = BindingMode.OneWay)]
	private Label _manaLabel = null!;

	[Export, BindTo("GoldText", Mode = BindingMode.OneWay)]
	private Label _goldLabel = null!;

	// Public interface for other systems (combat system, etc.) to call
	public HudViewModel Hud => ViewModel!;

	public override void _Ready()
	{
		ViewModel = new HudViewModel();
		DotPudicaInitialize();

		// Subscribe to death notification
		DotPudica.Core.Messaging.MessageBus.Register<HudView,
			DotPudica.Core.Messaging.NotificationMessage>(this, (view, msg) =>
		{
			if (msg.Key == "PlayerDead")
				GD.Print("[HUD] Player died, triggering Game Over screen...");
		});
	}

	// Quick test: simulate combat in Godot via key input
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed)
		{
			switch (key.Keycode)
			{
				case Key.Q: ViewModel?.TakeDamage(10); break;   // Press Q to take damage
				case Key.W: ViewModel?.ConsumeMana(15); break;  // Press W to consume mana
				case Key.E: ViewModel?.PickupGold(100); break;  // Press E to pick up gold
			}
		}
	}

    public override void _ExitTree()
    {
        DotPudicaDispose();
        base._ExitTree();
    }
}
