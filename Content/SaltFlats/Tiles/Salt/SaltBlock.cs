using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.SaltFlats.Tiles.Salt;

public abstract class SaltBlock : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.ChecksForMerge[Type] = true;

		this.Merge(TileID.IceBlock, TileID.SnowBlock, TileID.Sand, ModContent.TileType<SaltBlockReflective>());

		DustType = DustID.Pearlsand;
		MineResist = 0.5f;
	}

	public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
		=> WorldGen.TileMergeAttempt(-2, TileID.IceBlock, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
}

public class SaltBlockDull : SaltBlock
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileBlendAll[Type] = true;
		Main.tileBlockLight[Type] = true;

		AddMapEntry(new Color(180, 170, 170));
	}

	public override void RandomUpdate(int i, int j)
	{
		if (Main.rand.NextBool(4))
			Placer.PlaceTile<Saltwort>(i, j - 1).Send();
	}

	public override void FloorVisuals(Player player)
	{
		if (!Main.gamePaused && (int)player.velocity.X != 0 && Main.rand.NextBool(4))
		{
			var velocity = player.velocity * 0.2f + Vector2.UnitY * -0.5f;
			var smoke = new SmokeCloud(player.Bottom, velocity, Color.White * 0.8f, Main.rand.NextFloat(0.02f, 0.1f), Common.Easing.EaseFunction.EaseCircularOut, 60)
			{
				Pixellate = true,
				PixelDivisor = 5,
				TertiaryColor = Color.HotPink
			};

			ParticleHandler.SpawnParticle(smoke);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => TileMerger.DrawMerge(spriteBatch, i, j, TileID.Dirt, TileID.Sand);
}

public class SaltBlockReflective : SaltBlock
{
	public override string Texture => DrawHelpers.RequestLocal(typeof(SaltBlock), nameof(SaltBlock));

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		AddMapEntry(new Color(230, 220, 220));
	}

	public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		SaltBlockVisuals.ReflectionPoints.Add(new(i, j));
		return true;
	}
}