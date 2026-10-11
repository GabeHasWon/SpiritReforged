using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class MummyReward : ModItem, IFlagged
{
	public class MummyRewardPlayer : ModPlayer
	{
		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (Player.CheckFlag("MummyReward") == true)
				modifiers.DisableDust();
		}

		public override void OnHurt(Player.HurtInfo info)
		{
			if (Player.CheckFlag("MummyReward") == true)
			{
				for (int i = 0; i < 6; ++i)
					Dust.NewDust(Player.position, Player.width, Player.head, DustID.Sand, Player.velocity.X, Player.velocity.Y);
			}
		}

		public override void PostUpdateRunSpeeds()
		{
			if (Player.CheckFlag("MummyReward") != true)
				return;

			float factor = Player.statLife / (float)Player.statLifeMax2;
			Player.maxRunSpeed *= 1 + factor * 0.75f;
			Player.accRunSpeed *= 1 + factor * 0.75f;
			Player.runAcceleration *= 1 + factor * 0.75f;
		}
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Aglet);
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;
		Item.value = Item.buyPrice(0, 1, 0, 0);
	}
}
