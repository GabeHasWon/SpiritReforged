using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Forest.Glyphs;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class EnchantedWorkbench : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileOreFinderPriority[Type] = 600;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
		TileObjectData.newTile.Origin = new(2, 3);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;

		AddMapEntry(new Color(50, 25, 55), CreateMapEntryName());
	}

	public override void MouseOver(int i, int j)
	{
		if (UISystem.IsActive<EnchantmentUI>())
			return;

		var p = Main.LocalPlayer;

		p.cursorItemIconEnabled = true;
		p.cursorItemIconID = ModContent.ItemType<ChromaticWax>();
		p.noThrow = 2;
	}

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
}