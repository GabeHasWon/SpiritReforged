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

		this.Merge(ModContent.TileType<SaltBlockReflective>(), TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand);
		AddMapEntry(new Color(180, 170, 170));
	}

	public override void RandomUpdate(int i, int j)
	{
		Tile above = Framing.GetTileSafely(i, j - 1);

		if (above.LiquidAmount < 20)
		{
			if (Main.rand.NextBool(16))
				Placer.Check(i, j - 1, ModContent.TileType<Saltwort>()).IsClear().Place().Send();

			if (Main.rand.NextBool(32))
				Placer.Check(i, j - 1, ModContent.TileType<SaltwortTall>()).IsClear().Place().Send();

			if (Main.rand.NextBool(8) && (above.HasTileType(ModContent.TileType<Saltwort>()) || above.HasTileType(ModContent.TileType<SaltwortTall>())))
			{
				Tile tile = Main.tile[i, j];
				Point16 result = new(126, 252);
				int random = Main.rand.Next(4);

				tile.TileFrameX = (short)(result.X + 18 * random);
				tile.TileFrameY = result.Y;

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendTileSquare(-1, i, j);
			}
		}
	}

	public override void FloorVisuals(Player player)
	{
		if (!Main.gamePaused && (int)player.velocity.X != 0 && Main.rand.NextBool(4) && !Main.dedServ)
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
		const int loop = 4; //The number of horizontal frames
		Tile tile = Main.tile[i, j];

		if (Main.rand.NextBool(10) && tile.TileFrameX is 18 or 36 or 54 && tile.TileFrameY is 18) //Plain center frames
		{
			Point16 result = new(126, 216);
			int random = Main.rand.Next(8);

			tile.TileFrameX = (short)(result.X + 18 * (random % loop));
			tile.TileFrameY = (short)(result.Y + 18 * (random / loop));
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Dirt, TileID.Sand);
}