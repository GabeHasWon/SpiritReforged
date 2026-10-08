namespace SpiritReforged.Content.Underground.Tiles.Mirror;

public sealed class LuckPlayer : ModPlayer
{
	public StatModifier luckModifier = StatModifier.Default;
	public int luckResetTime;

	public override void Load() => On_Player.RecalculateLuck += PostRecalculateLuck;

	private static void PostRecalculateLuck(On_Player.orig_RecalculateLuck orig, Player self)
	{
		orig(self);

		if (self.whoAmI == Main.myPlayer && self.TryGetModPlayer(out LuckPlayer luckP))
			self.luck = luckP.luckModifier.ApplyTo(self.luck);
	}

	public override void ResetEffects()
	{
		if (Player.whoAmI == Main.myPlayer && --luckResetTime <= 0)
		{
			StatModifier lastLuckModifier = luckModifier;
			luckModifier = StatModifier.Default;

			if (lastLuckModifier != luckModifier)
				Player.luckNeedsSync = true;
		}
	}
}