using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Forest.Misc.OtherworldlyRadio;

public class OtherworldlyRadioItem : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(shop => shop.NpcType == NPCID.PartyGirl, new NPCShop.Entry(Type)));
		Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });
		Item.ResearchUnlockCount = 1;
	}

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<OtherworldlyRadioPlaced>());
		Item.width = 30;
		Item.height = 32;
		Item.value = Item.sellPrice(gold: 3);
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = 1;
	}

	public override bool CanRightClick() => true;

	public override void RightClick(Player player)
	{
		Main.swapMusic = !Main.swapMusic;
		Item.stack++;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		frame.Y = (frame.Height + 2) * (Main.swapMusic ? 0 : 1);
		spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, Item.GetAlpha(drawColor), 0, origin, scale, default, 0);
		return false;
	}
}

public class OtherworldlyRadioPlaced : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidWithTop | AnchorType.SolidTile | AnchorType.Table, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(142, 92, 79), this.GetLocalization("MapEntry"));
		RegisterItemDrop(ModContent.ItemType<OtherworldlyRadioItem>());

		AdjTiles = [TileID.MusicBoxes];
		DustType = DustID.WoodFurniture;
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
		HitWire(i, j);
		return true;
	}

	public override void HitWire(int i, int j) => Main.swapMusic = !Main.swapMusic;
	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData) => MusicBoxTile.SpawnMusicNoteVFX(i, j, Main.swapMusic);
	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
}