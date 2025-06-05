using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Forest.Cartography;

public class CartographyTable : ModTile, IAutoloadTileItem
{
	public void SetItemDefaults(ModItem item) => item.Item.value = Item.buyPrice(gold: 1, silver: 20);

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(191, 142, 111), this.AutoModItem().DisplayName);
		DustType = DustID.WoodFurniture;
	}

	public override bool RightClick(int i, int j)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return false;

		MappingSystem.SetMap();
		Main.NewText(Language.GetTextValue("Mods.SpiritReforged.Misc.UpdateMap"), new Color(255, 240, 20));

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		var p = Main.LocalPlayer;
		p.cursorItemIconEnabled = true;
		p.cursorItemIconID = this.AutoItem().type;
		p.noThrow = 2;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (MappingSystem.MapUpdated && TileObjectData.IsTopLeft(i, j))
		{
			var texture = TextureAssets.QuicksIcon.Value;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			float y = (float)Math.Sin(Main.timeForVisualEffects / 20f) * 2f;
			var position = new Vector2(i, j).ToWorldCoordinates(24, -8 + y) - Main.screenPosition + TileExtensions.TileOffset;

			spriteBatch.Draw(texture, position, null, Color.White, 0, texture.Size() / 2, 1, default, 0);
			spriteBatch.Draw(bloom, position, null, Color.Orange.Additive() * 0.3f, 0, bloom.Size() / 2, 0.2f, default, 0);
		}
	}
}