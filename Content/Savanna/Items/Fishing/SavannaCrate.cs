using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using System.Linq;

namespace SpiritReforged.Content.Savanna.Items.Fishing;

public class SavannaCrate : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 10;

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SavannaCrateTile>());
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		int[] dropOptions = [ModContent.ItemType<HuntingRifle.HuntingRifle>(),
			ItemID.SandstorminaBottle,
			ItemID.AnkletoftheWind,
			ItemID.MysticCoilSnake,
			ItemID.FeralClaws];

		if (CrossMod.Fables.Enabled)
		{
			if (CrossMod.Fables.TryFind("ToxicBlowpipe", out ModItem toxicBLowpipe))
				dropOptions = dropOptions.Append(toxicBLowpipe.Type).ToArray();
			if (CrossMod.Fables.TryFind("CrackshotColt", out ModItem crackshotColt))
				dropOptions = dropOptions.Append(crackshotColt.Type).ToArray();
		}

		var main = ItemDropRule.OneFromOptions(1, dropOptions);

		CrateHelper.BiomeCrate(itemLoot, main, ItemDropRule.NotScalingWithLuck(ItemID.BambooBlock, 3, 20, 50), 
			ItemDropRule.NotScalingWithLuck(ItemID.DesertFossil, 3, 20, 50), ItemDropRule.NotScalingWithLuck(ItemID.Leather, 3, 5, 10));
	}
}

public class SavannaCrateTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileSolidTop[Type] = true;
		Main.tileTable[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
		AddMapEntry(new Color(123, 104, 84));
		DustType = -1;
	}
}