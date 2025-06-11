using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.NPCs.Sparrow;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooBirdCage : SingleSlotTile<BambooBirdCageSlot>, IAutoloadTileItem
{
	internal static HashSet<int> BirdTypes;

	public void SetItemDefaults(ModItem item)
	{
		item.Item.DefaultToPlaceableTile(ModContent.TileType<BambooBirdCage>());
		item.Item.width = 20;
		item.Item.height = 32;
		item.Item.value = 50;
	}

	public void AddItemRecipes(ModItem item) => item.CreateRecipe()
		.AddIngredient(AutoContent.ItemType<StrippedBamboo>(), 14).AddTile(TileID.Sawmill).Register();

	public override void SetStaticDefaults()
	{
		BirdTypes = [ItemID.Cardinal, ItemID.BlueJay, ItemID.GoldBird, ItemID.Bird, ItemID.Seagull, 
			ItemID.BlueMacaw, ItemID.GrayCockatiel, AutoContent.ItemType<Sparrow>()];

		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 2, 0);
		ModTileEntity tileEntity = ModContent.GetInstance<BambooBirdCageSlot>();
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 2, 0);
		TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(100, 100, 60), name);
		DustType = -1;
		RegisterItemDrop(ItemType);
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
	{
		if (tileFrameX >= 18 * 2) //Ceiling mounted
			offsetY = -4;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && Entity(i, j) is BambooBirdCageSlot slot && !slot.item.IsAir && Main.rand.NextBool(100))
			SoundEngine.PlaySound(SoundID.Bird, new Vector2(i * 16, j * 16));
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (TileObjectData.IsTopLeft(i, j) && Entity(i, j) is BambooBirdCageSlot slot && !slot.item.IsAir)
		{
			var bird = TextureAssets.Item[slot.item.type];
			var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;

			position += new Vector2(16, tile.TileFrameX >= 18 * 2 ? 42 : 50);

			if (((int)Main.GlobalTimeWrappedHourly + i + j) % 10 == 0)
				position.Y -= 2; //Periodically hop

			spriteBatch.Draw(bird.Value, position, null, Lighting.GetColor(i, j), 0, bird.Frame().Bottom(), 1, SpriteEffects.None, 0);
		}

		return true;
	}
}

public class BambooBirdCageSlot : SingleSlotEntity
{
	public override bool IsTileValidForEntity(int x, int y)
	{
		var tile = Framing.GetTileSafely(x, y);
		return tile.HasTile && tile.TileType == ModContent.TileType<BambooBirdCage>() && TileObjectData.IsTopLeft(x, y);
	}

	public override bool CanAddItem(Item item) => BambooBirdCage.BirdTypes.Contains(item.type);
	public override void Update() { }
}
