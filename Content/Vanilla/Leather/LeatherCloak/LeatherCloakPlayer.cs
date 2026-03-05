using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Vanilla.Leather.LeatherCloak;

internal class LeatherCloakPlayer : ModPlayer
{
	public override void PostUpdateRunSpeeds()
	{
		if (Player.HasEquip<LeatherCloakItem>())
		{
			Player.runAcceleration *= 1.15f;
			Player.maxRunSpeed += 0.1f;
			Player.accRunSpeed += 0.05f;
		}
	}
}