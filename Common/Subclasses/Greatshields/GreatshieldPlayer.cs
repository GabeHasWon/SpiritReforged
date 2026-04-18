using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal class GreatshieldPlayer : ModPlayer
{
	public int LastDefense { get; private set; } // Last frame's defense for use in GreatshieldClass's damage boost

	public float AnimationFactor => parryAnim / (float)parryAnimMax;

	internal int parryTime = 0; // How long between parries
	internal int parryTimeMax = 0;
	internal int decayWait = 0; // How long until the boosted health decays
	internal int decayTimer = 0; // Timer for decaying boosted health
	internal int boostHealth = 0; // How much added health the player has from guarding
	internal int parryAnim = 0; // How long the parry animation lasts
	internal int parryAnimMax = 0;

	public override void ResetEffects()
	{
		parryTime = Math.Max(parryTime - 1, 0);
		decayWait = Math.Max(decayWait - 1, 0);
		parryAnim = Math.Max(parryAnim - 1, 0);
	}

	public override void PostUpdateEquips() 
	{
		LastDefense = Player.statDefense;
		Player.statLifeMax2 += boostHealth;

		if (decayWait <= 0 && boostHealth > 0)
		{
			decayTimer++;

			if (decayTimer % 15 == 0)
			{
				boostHealth = Math.Max(boostHealth - 1, 0);
				Player.statLife = Math.Max(Player.statLife - 1, 0);
			}
		}
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (boostHealth <= 0)
			return;

		modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
		{
			int dif = Math.Min(info.Damage - boostHealth, info.Damage);
			boostHealth -= info.Damage;

			if (boostHealth > 0)
				Player.statLife += info.Damage;
		};
	}

	internal void Guard(GreatshieldAltInfo info)
	{
		if (boostHealth > info.BoostHealth)
			return;

		int dif = info.BoostHealth - boostHealth;

		parryTimeMax = parryTime = info.ParryTime;
		decayWait = info.DelayDecay;
		boostHealth = info.BoostHealth;
		parryAnim = parryAnimMax = info.AnimationTime;

		Player.statLifeMax2 += boostHealth;
		Player.statLife += dif;
	}

	public override void HideDrawLayers(PlayerDrawSet drawInfo)
	{
		if (drawInfo.drawPlayer.HeldItem.ModItem is GreatshieldItem)
			PlayerDrawLayers.Shield.Hide();
	}
}
