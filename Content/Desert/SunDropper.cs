using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Content.Desert.Oasis;
using SpiritReforged.Content.Desert.Tiles.Amber;
using System.Linq;

namespace SpiritReforged.Content.Desert;

public class SunDropper : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 99;
	public override void SetDefaults() => Item.CloneDefaults(ItemID.MagicHoneyDropper);

	public override bool CanUseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer && player.IsTargetTileInItemRange(Item))
		{
			int i = Player.tileTargetX;
			int j = Player.tileTargetY;

			return WorldGen.SolidTile(i, j);
		}

		return true;
	}

	public override bool? UseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			int i = Player.tileTargetX;
			int j = Player.tileTargetY;

			var position = new Vector2(i, j).ToWorldCoordinates();

			if (SimpleEntitySystem.Entities.Count(x => x is LightShaft && x.Center == position) <= 3) //Don't let them stack too much
				SimpleEntitySystem.NewEntity<LightShaft>(position);

			return true;
		}

		return null;
	}

	public override void AddRecipes() => CreateRecipe(8)
		.AddIngredient(ItemID.EmptyDropper, 8)
		.AddIngredient(AutoContent.ItemType<PolishedAmber>())
		.AddTile(TileID.CrystalBall)
		.Register();
}