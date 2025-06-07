using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Utilities;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaCampfire : ModTile, IAutoloadTileItem
{
	private static Asset<Texture2D> glowTexture;
	private const int fullFrameHeight = 18 * 2;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 10).AddIngredient(ModContent.ItemType<SavannaTorchItem>(), 5).Register();

	private static bool OnFire(int i, int j) => Main.tile[i, j].TileFrameY < fullFrameHeight;

	public override void Load() => glowTexture = ModContent.Request<Texture2D>(Texture + "_Glow");

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.Campfire[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.Campfire, 0));
		TileObjectData.newTile.StyleLineSkip = 9;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(254, 121, 2), Language.GetText("ItemName.Campfire"));
		DustType = -1;
		AdjTiles = [TileID.Campfire];
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (OnFire(i, j))
			Main.SceneMetrics.HasCampfire = true;
	}

	public override void MouseOver(int i, int j)
	{
		var player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;

		int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
		player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
	}

	public override bool RightClick(int i, int j)
	{
		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
		HitWire(i, j);

		return true;
	}

	public override void HitWire(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		short frameAdjustment = (short)(!OnFire(i, j) ? -fullFrameHeight : fullFrameHeight);

		for (int x = i; x < i + 3; x++)
			for (int y = j; y < j + 2; y++)
			{
				Main.tile[x, y].TileFrameY += frameAdjustment;

				if (Wiring.running)
					Wiring.SkipWire(x, y);
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
		if (Main.gamePaused || !Main.instance.IsActive)
			return;

		if (!Lighting.UpdateEveryFrame || new FastRandom(Main.TileFrameSeed).WithModifier(i, j).Next(4) == 0)
		{
			var tile = Main.tile[i, j];
			if (tile.TileFrameY == 0 && Main.rand.NextBool(3) && (Main.drawToScreen && Main.rand.NextBool(4) || !Main.drawToScreen))
			{
				var dust = Dust.NewDustDirect(new Vector2(i * 16 + 2, j * 16 - 4), 4, 8, DustID.Smoke, 0f, 0f, 100);
				if (tile.TileFrameX == 0)
					dust.position.X += Main.rand.Next(8);

				if (tile.TileFrameX == fullFrameHeight)
					dust.position.X -= Main.rand.Next(8);

				dust.alpha += Main.rand.Next(100);
				dust.velocity *= 0.2f;
				dust.velocity.Y -= 0.5f + Main.rand.Next(10) * 0.1f;
				dust.fadeIn = 0.5f + Main.rand.Next(10) * 0.1f;
			}
		}
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (OnFire(i, j))
		{
			float pulse = Main.rand.Next(28, 42) * .005f + (270 - Main.mouseTextColor) / 700f;
			(r, g, b) = (.9f + pulse, .4f + pulse, .1f + pulse);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (!TileDrawing.IsVisible(tile))
			return;

		if (OnFire(i, j))
		{
			int addFrameX = 0, addFrameY = 0;

			TileLoader.SetAnimationFrame(Type, i, j, ref addFrameX, ref addFrameY);

			var source = new Rectangle(tile.TileFrameX, tile.TileFrameY + addFrameY, 16, 16);
			var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

			spriteBatch.Draw(glowTexture.Value, position, source, Color.White with { A = 0 }, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		}
	}
}