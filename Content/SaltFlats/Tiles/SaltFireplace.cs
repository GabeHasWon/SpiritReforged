using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.SaltFlats.Tiles;

[AutoloadGlowmask("255,255,255", false)]
public class SaltFireplace : ModTile, IAutoloadTileItem
{
	private const int fullFrameHeight = 18 * 2;

	private static bool OnFire(int i, int j) => Main.tile[i, j].TileFrameY < fullFrameHeight;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(AutoContent.ItemType<SaltBlockDull>(), 14).AddIngredient(ModContent.ItemType<SaltFlatsTorchItem>(), 5).AddTile(TileID.WorkBenches).Register();

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.Campfire, 0));
		TileObjectData.newTile.StyleLineSkip = 9;
		TileObjectData.addTile(Type);

		AddMapEntry(FurnitureTile.CommonColor, Language.GetText("ItemName.Campfire"));
		AdjTiles = [TileID.Fireplace];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (OnFire(i, j))
			Main.SceneMetrics.HasCampfire = true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;

		int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
		player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i, j).ToWorldCoordinates());
		HitWire(i, j);

		return true;
	}

	public override void HitWire(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		short frameAdjustment = (short)(!OnFire(i, j) ? -fullFrameHeight : fullFrameHeight);

		for (int x = i; x < i + 3; x++)
		{
			for (int y = j; y < j + 2; y++)
			{
				Main.tile[x, y].TileFrameY += frameAdjustment;

				if (Wiring.running)
					Wiring.SkipWire(x, y);
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, i, 3, 2);
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 8;
		}
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		if (OnFire(i, j))
			frameYOffset = Main.tileFrame[type] * fullFrameHeight;
		else
			frameYOffset = 252;
	}

	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (OnFire(i, j))
		{
			float pulse = Main.rand.Next(28, 42) * 0.005f + (270 - Main.mouseTextColor) / 700f;
			(r, g, b) = (0.7f + pulse, 0.7f + pulse, 0.8f + pulse);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (TileDrawing.IsVisible(tile) && OnFire(i, j))
		{
			int addFrameX = 0, addFrameY = 0;
			TileLoader.SetAnimationFrame(Type, i, j, ref addFrameX, ref addFrameY);

			Rectangle source = new(tile.TileFrameX, tile.TileFrameY + addFrameY, 16, 16);
			Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

			spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, position, source, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		}
	}
}