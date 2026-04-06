using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

[AutoloadGlowmask("255,255,255", false)]
public class EnchantedAnvil : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileOreFinderPriority[Type] = 600;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;

		AddMapEntry(new Color(50, 25, 55), CreateMapEntryName());
		RegisterItemDrop(ItemID.IronAnvil, 0);
		RegisterItemDrop(ItemID.LeadAnvil, 1);
	}

	public override void MouseOver(int i, int j) => base.MouseOver(i, j);

	public override bool RightClick(int i, int j)
	{
		UISystem.SetActive<EnchantmentUI>();
		Main.playerInventory = true;

		return true;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer && TileObjectData.IsTopLeft(i, j))
		{
			if (Main.LocalPlayer.DistanceSQ(new Vector2(i + 1, j) * 16) > 100 * 100)
				UISystem.SetInactive<EnchantmentUI>();
		}
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{

	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		Texture2D texture = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;

		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2), 
			new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16), Color.Cyan, 0, Vector2.Zero, 1, 0, 0);
	}
}