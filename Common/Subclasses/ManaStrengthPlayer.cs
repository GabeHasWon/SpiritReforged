namespace SpiritReforged.Common.Subclasses;

public class ManaStrengthPlayer : ModPlayer
{
	public int totalManaConsumed;
	private int _lastHeldType;

	public override void PostUpdateEquips()
	{
		//if (Player.ItemAnimationActive) //Only check against item type when an item is being used
		//{
			int type = Player.HeldItem.type;
			if (type != _lastHeldType)
			{
				totalManaConsumed = 0;
				_lastHeldType = type;
			}
		//}
	}

	public override void OnConsumeMana(Item item, int manaConsumed) => totalManaConsumed += manaConsumed;

	public override void OnMissingMana(Item item, int neededMana) => totalManaConsumed = 0;
}