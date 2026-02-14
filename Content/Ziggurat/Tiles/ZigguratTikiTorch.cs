using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class ZigguratTikiTorch : ModTile, IAutoloadTileItem
{
	public const int FrameWidth = 18;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(AutoContent.ItemType<RedSandstoneBrick>(), 4).AddIngredient(ItemID.Torch, 1).Register();

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(174, 110, 48));
		RegisterItemDrop(this.AutoItemType());

		DustType = -1;
		this.AutoItem().ResearchUnlockCount = 5;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override bool RightClick(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		SoundEngine.PlaySound(SoundID.Mech, new Vector2(i, j).ToWorldCoordinates());
		HitWire(i, j);

		return true;
	}

	public override void HitWire(int i, int j)
	{
		bool activated = false;
		for (int y = 0; y < 2; y++)
		{
			Tile tile = Main.tile[i, j + y];
			tile.TileFrameX = (short)((activated = tile.TileFrameX == 0) ? FrameWidth : 0);
		}

		var sound = activated ? ZigguratTorch.Extinguish : ZigguratTorch.Ignite;
		SoundEngine.PlaySound(sound, new Vector2(i, j).ToWorldCoordinates());

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 1, 2);
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer)
			return;

		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY == 0 && tile.TileFrameX == 0)
		{
			Vector2 worldCoords = new Vector2(i, j).ToWorldCoordinates();

			if (Main.LocalPlayer.Distance(worldCoords) < 16 * 3 && Wiring.CheckMech(i, j, 180))
			{
				HitWire(i, j);

				for (int x = 0; x < 5; x++)
					ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.AshTreeShake, new() { PositionInWorld = worldCoords - new Vector2(0, 8) });
			}
		}
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (tile.TileFrameX != 0 && tile.TileFrameY == 0)
		{
			if (Main.rand.NextBool())
			{
				var dust = Dust.NewDustDirect(new Vector2(i, j).ToWorldCoordinates(0, -4), 16, 2, DustID.Torch, Scale: Main.rand.NextFloat(1, 3));
				dust.noGravity = true;
				dust.velocity.Y = -Main.rand.NextFloat(3);
				dust.fadeIn = 1;
			}

			if (Main.rand.NextBool())
			{
				var dust2 = Dust.NewDustDirect(new Vector2(i, j).ToWorldCoordinates(0, -8), 16, 2, DustID.Smoke, Alpha: 100, Scale: Main.rand.NextFloat(1, 2));
				dust2.noGravity = true;
				dust2.velocity = Vector2.UnitY.RotatedByRandom(0.5f) * -3;
				dust2.fadeIn = 1;
			}
		}
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX != 0 && tile.TileFrameY == 0)
		{
			float pulse = Main.rand.Next(28, 42) * 0.005f + (270 - Main.mouseTextColor) / 700f;
			(r, g, b) = (0.9f + pulse, 0.4f + pulse, 0.1f + pulse);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (TileDrawing.IsVisible(tile) && tile.TileFrameY == 0 && tile.TileFrameX != 0)
		{
			Texture2D texture = TextureAssets.Flames[0].Value;
			Rectangle source = new(0, 0, 22, 22);
			var position = new Vector2(i, j).ToWorldCoordinates(8 + Main.rand.NextFloat(-1f, 1f) * 1.5f, 4) - Main.screenPosition + TileExtensions.TileOffset;

			spriteBatch.Draw(texture, position, source, Color.White.Additive(50), 0, source.Size() / 2, 1.3f, SpriteEffects.None, 0);

			DrawFlame(spriteBatch, (int)((Main.timeForVisualEffects + j * 0.1f) / 6f) % 5, i, j, Color.White.Additive(50) * 0.5f);
			DrawFlame(spriteBatch, (int)((Main.timeForVisualEffects + j * 0.1f) / 6f) % 5, i, j, Color.White.Additive(50) * 0.5f);
		}

		static void DrawFlame(SpriteBatch spriteBatch, int frame, int i, int j, Color color)
		{
			Texture2D texture = ZigguratTorch.Flame.Value;
			var source = texture.Frame(1, 5, 0, frame, 0, -2);
			var position = new Vector2(i, j).ToWorldCoordinates(8 + Main.rand.NextFloat(-1f, 1f) * 1.5f, 2) - Main.screenPosition + TileExtensions.TileOffset;

			spriteBatch.Draw(texture, position, source, color, 0, new Vector2(source.Width / 2, source.Height), 1, SpriteEffects.None, 0);
		}
	}
}