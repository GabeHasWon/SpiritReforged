using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Savanna.Biome;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaFountain : ModTile, IAutoloadTileItem
{
	private const int FrameHeight = 72;

	void IAutoloadTileItem.SetItemDefaults(ModItem item) => item.Item.SetShopValues(ItemRarityColor.Blue1, Item.buyPrice(0, 4, 0, 0));

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.Stone;
		AnimationFrameHeight = FrameHeight;
		AdjTiles = [TileID.WaterFountain];

		int itemId = Mod.Find<ModItem>("SavannaFountainItem").Type;
		RegisterItemDrop(itemId);
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(shop => shop.NpcType == NPCID.WitchDoctor, new NPCShop.Entry(itemId)));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override void AnimateTile(ref int frame, ref int frameCounter) => frame = Main.tileFrame[TileID.WaterFountain];

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		var tile = Main.tile[i, j];
		if (tile.TileFrameY >= FrameHeight)
			frameYOffset = Main.tileFrame[type] * FrameHeight;
		else
			frameYOffset = 0; //Don't animate when turned off
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.dedServ && Main.tile[i, j].TileFrameY >= FrameHeight)
			Main.LocalPlayer.GetModPlayer<FountainPlayer>().SetFountain<SavannaWaterStyle>();
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
		ToggleTile(i, j);
		return true;
	}

	public override void HitWire(int i, int j) => ToggleTile(i, j);

	private void ToggleTile(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		for (int x = i; x < i + 2; x++)
		{
			for (int y = j; y < j + 4; y++)
			{
				Tile tile = Framing.GetTileSafely(x, y);

				if (tile.HasTile && tile.TileType == Type)
				{
					if (tile.TileFrameY < FrameHeight)
						tile.TileFrameY += FrameHeight;
					else
						tile.TileFrameY -= FrameHeight;
				}

				Wiring.SkipWire(x, y);
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 2, 4);
	}
}
