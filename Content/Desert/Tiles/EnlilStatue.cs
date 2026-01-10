using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.Tiles;

public class EnlilStatue : ModTile, IAutoloadTileItem
{
	public class EnlilBuff : ModBuff
	{
		public override void SetStaticDefaults() => Main.pvpBuff[Type] = true;
		public override void Update(Player player, ref int buffIndex)
		{
			player.jumpSpeedBoost += 0.8f;
			player.maxFallSpeed *= 1.5f;
			player.gravity += 0.05f;
		}
	}

	public static float GetOpacity(int x, int y) => 1f - Math.Clamp(Main.LocalPlayer.Distance(new Vector2(x, y).ToWorldCoordinates(16, 32)) / 100f, 0, 1);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new(0, 2);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = DustID.Stone;
		AddMapEntry(new Color(107, 90, 64), CreateMapEntryName());
		RegisterItemDrop(this.AutoItemType());
	}

	public override bool RightClick(int i, int j)
	{
		Main.LocalPlayer.AddBuff(ModContent.BuffType<EnlilBuff>(), 60 * 60 * 3);
		SoundEngine.PlaySound(SoundID.AbigailUpgrade, new Vector2(i, j).ToWorldCoordinates());

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.cursorItemIconEnabled = true;
		player.noThrow = 2;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8))
		{
			float opacity = GetOpacity(i, j);

			if (opacity > 0)
			{
				Vector2 position = Main.rand.NextVector2FromRectangle(new(i * 16, (j + 3) * 16, 32, 2));
				float magnitude = Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new EmberParticle(position, Vector2.UnitY * -magnitude, Color.Goldenrod * opacity, (1f - magnitude) * opacity, Main.rand.Next(30, 120), 2));
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
		{
			float opacity = GetOpacity(i, j);

			if (opacity > 0)
				GlowTileHandler.AddGlowPoint(new Rectangle(i, j + 2, 32, 16), Color.Goldenrod * opacity * 0.5f);
		}

		return true;
	}
}