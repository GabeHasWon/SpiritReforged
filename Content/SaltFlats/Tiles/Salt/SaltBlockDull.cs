using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public class SaltBlockDull : SaltBlock
{


	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		Main.tileBlockLight[Type] = true;

		this.Merge(ModContent.TileType<SaltBlockReflective>());
		AddMapEntry(new Color(180, 170, 170));
	}

	public override void RandomUpdate(int i, int j)
	{
		if (Framing.GetTileSafely(i, j - 1).LiquidAmount < 20)
		{
			if (Main.rand.NextBool(4))
				Placer.Check(i, j - 1, ModContent.TileType<Saltwort>()).IsClear().Place().Send();

			if (Main.rand.NextBool(8))
				Placer.Check(i, j - 1, ModContent.TileType<SaltwortTall>()).IsClear().Place().Send();
		}
	}

	public override void FloorVisuals(Player player)
	{
		if (!Main.gamePaused && (int)player.velocity.X != 0 && Main.rand.NextBool(4))
		{
			var velocity = player.velocity * 0.2f + Vector2.UnitY * -0.5f;
			var position = player.Bottom + new Vector2(0, 6 * player.gravDir);

			var smoke = new SmokeCloud(position, velocity, Color.White * 0.8f, Main.rand.NextFloat(0.02f, 0.05f), Common.Easing.EaseFunction.EaseCircularOut, 60)
			{
				Pixellate = true,
				PixelDivisor = 5,
				TertiaryColor = Color.HotPink
			};

			ParticleHandler.SpawnParticle(smoke);
		}
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		var t = Main.tile[i, j];

		if (Main.rand.NextBool(15) && t.TileFrameX is 18 or 36 or 54 && t.TileFrameY is 18) //Plain center frames
		{
			Point16 result = new(126, 216);
			int random = Main.rand.Next(4);

			t.TileFrameX = (short)(result.X + 18 * random);
			t.TileFrameY = result.Y;
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Dirt, TileID.Sand);
}