using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class BoxOfSpareLimbs : ModItem, IFlagged
{
	public class SpareLimbPlayer : ModPlayer
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
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Aglet);
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;
		Item.value = Item.buyPrice(0, 1, 0, 0);
	}
}
