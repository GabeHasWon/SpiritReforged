using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Snow.Frostbite;

public class FrozenBooks : ModTile
{
	public const int Styles = 10;
	public static bool IsTome(int i, int j) => Main.tile[i, j].TileFrameX >= 18 * (Styles - 1);

	public override void SetStaticDefaults()
	{
		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleOnTable1x1);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleWrapLimit = Styles;
		TileObjectData.addTile(Type);

		RegisterItemDrop(ItemID.Book, 0, 1, 2, 3, 4, 5, 6, 7, 8);
		RegisterItemDrop(ModContent.ItemType<FrostbiteItem>(), Styles - 1);
		DustType = -1;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (IsTome(i, j))
			SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = .2f }, new Vector2(i, j).ToWorldCoordinates());

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		if (IsTome(i, j))
		{
			Player player = Main.LocalPlayer;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<FrostbiteItem>();
			player.noThrow = 2;
		}
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int maxDistance = 160;

		if (IsTome(i, j))
		{
			WindSoundPlayer.StartSound(new Vector2(i, j).ToWorldCoordinates());

			var world = new Vector2(i, j).ToWorldCoordinates();
			float distance = Main.LocalPlayer.Distance(world);

			if (closer && !Main.gamePaused && distance < maxDistance)
				DoVisuals(world, Math.Min(1f - distance / maxDistance, .35f));
		}
	}

	private static void DoVisuals(Vector2 world, float progress)
	{
		var color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(.75f)).Additive(180);

		for (int i = 0; i < 2; i++)
		{
			ParticleHandler.SpawnParticle(new DissipatingImage(GetContinuous(world), color * 0.1f * progress, Main.rand.NextFloat(MathHelper.Pi), 2.5f, Main.rand.NextFloat(0.5f), "SmokeSimple", new(0.6f, 0.6f), new(3, 1.5f), 40)
			{
				UseLightColor = true,
				Pixellate = true,
				PixelDivisor = 10f
			});

			var pos = GetContinuous(world) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f);
			var vel = world.DirectionTo(pos).RotatedBy(MathHelper.PiOver2).RotatedByRandom(.1f) * Main.rand.NextFloat(1f, 5f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, Color.White * progress, Color.CornflowerBlue, Main.rand.NextFloat(0.15f, 0.45f), Main.rand.Next(30, 50), 1, delegate (Particle p)
			{
				p.Velocity *= .95f;
				p.Velocity = p.Velocity.RotatedBy(p.Position.AngleTo(world) * .025f);
			}));
		}

		if (Main.rand.NextBool(5))
		{
			ParticleHandler.SpawnParticle(new SnowflakeParticle(GetContinuous(world), Vector2.UnitY * Main.rand.NextFloat(), Color.White * .3f * progress, Color.RoyalBlue * .6f * progress, Main.rand.NextFloat(.5f), 60, 0, Main.rand.Next(3), delegate (Particle p)
			{
				p.Velocity *= .98f;
				p.Velocity = p.Velocity.RotatedByRandom(.05f);
				p.Rotation += p.Velocity.Length() * .025f;
			}));
		}

		static Vector2 GetContinuous(Vector2 from)
		{
			const int maxTries = 10;
			var to = from;

			for (int i = 0; i < maxTries; i++)
			{
				to = from + Main.rand.NextVector2Unit() * Main.rand.NextFloat(120);
				if (Collision.CanHitLine(from, 2, 2, to, 2, 2))
					break;
			}

			return to;
		}
	}
}