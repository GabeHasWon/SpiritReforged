namespace SpiritReforged.Content.SaltFlats.Items.Crates;

public class SaltCrateRestored : ModItem
{
	public class SaltCrateRestoredTile : ModTile
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

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 10;
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateRestoredTile>());
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(0, 1, 0, 0);
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot) => SaltCrate.ModifyLoot(itemLoot);
}