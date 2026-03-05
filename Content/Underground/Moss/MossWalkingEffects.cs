using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.Moss;

internal class MossWalkingEffects : GlobalTile
{
	public override void FloorVisuals(int type, Player player)
	{
		if (Main.gamePaused)
			return;

		int chance = (int)Math.Clamp(45 - 7.5f * player.velocity.Length(), 1, 45);

		if (chance >= 1 && Main.rand.NextBool(chance))
		{
			if (type is TileID.XenonMoss or TileID.XenonMossBrick)
				SpawnXenonParticles(player);
			else if (type is TileID.ArgonMoss or TileID.ArgonMossBrick)
				SpawnArgonParticles(player);
			else if(type is TileID.KryptonMoss or TileID.KryptonMossBrick)
				SpawnKryptonParticles(player);
			else if (type is TileID.VioletMoss or TileID.VioletMossBrick)
				SpawnNeonParticles(player);
			else if (type is TileID.RainbowMoss or TileID.RainbowMossBrick)
				SpawnHeliumParticles(player);
			else if (type is TileID.LavaMoss or TileID.LavaMossBrick)
				SpawnLavaParticles(player);
			else if (type == ModContent.TileType<RadonMoss>() || type == ModContent.TileType<RadonMossGrayBrick>())
				SpawnRadonParticles(player);
			else if (type == ModContent.TileType<OganessonMoss>() || type == ModContent.TileType<OganessonMossGrayBrick>())
				SpawnOganessonParticles(player);
		}
	}

	//Xenon: Simple rise and linger
	internal static void SpawnXenonParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), -1).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.4f, 1f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(0, 184, 255) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 180, 8, p =>
			{
				p.Velocity.X *= Main.rand.NextFloat(.8f, .9f);
				p.Velocity.Y *= Main.rand.NextFloat(.96f, .99f);
			}));
	}

	//Argon: Spirals based on player position and movement
	internal static void SpawnArgonParticles(Player player)
	{
		var center = player.Center;
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = (start - center).SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(.5f, 1f);

		float distance = Vector2.Distance(start, center);
		float rotationDir = 1f;

		if (distance < 100f)
			rotationDir *= Math.Sign(player.velocity.X);
		else
			rotationDir = 1f;

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(255, 92, 160) * 0.65f, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(90, 140), 8, p =>
			{
				var toCenter = (center - p.Position).SafeNormalize(Vector2.Zero);

				p.Velocity = p.Velocity.RotatedBy(0.2f * rotationDir) * 0.97f;
				p.Velocity += toCenter * 0.09f;
			}));
	}

	//Krypton: flits back and forth energetically while rising
	internal static void SpawnKryptonParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), -1).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1f, 2f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(105, 255, 41) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 120, 7, p =>
			{
				if (Main.rand.NextBool(24))
					p.Velocity.X += Main.rand.NextFloat(-2f, 2f);

				if (Math.Abs(p.Velocity.X) > 1f)
					p.Velocity.X *= .9f;

				p.Velocity *= .96f;
			}));
	}

	//Neon: Slowly drift toward players
	internal static void SpawnNeonParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.4f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(210, 97, 255) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(95, 135), 8, p =>
			{
				var toPlayer = (player.Center - p.Position).SafeNormalize(Vector2.Zero);
				p.Velocity += toPlayer * 0.015f;

				p.Velocity.Y *= 0.98f;
				p.Velocity.X *= 0.97f;
			}));
	}

	//Helium: Random circles (i ran out of ideas lol + it's already rainbow)
	internal static void SpawnHeliumParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.4f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			Main.DiscoColor, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(95, 135), 8, p =>
			{
				p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-.3f, .3f)) * 0.98f;
				p.Color = Main.DiscoColor;
			}));
	}

	//Lava: Rise then fall
	internal static void SpawnLavaParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(0, -1).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(.25f, .5f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(252, 90, 3) * 0.85f, Main.rand.NextFloat(0.25f, 0.45f), 90, 4, p =>
			{
				p.Velocity.Y += Main.rand.NextFloat(.02f, .03f);
			}));
	}

	//Radon: Straight upwards in random direction, pulsates and lasts longer than all other mosses
	internal static void SpawnRadonParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(Main.rand.NextFloat(-.5f, .5f), Main.rand.NextFloat(-1.2f, -0.3f));

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(248, 255, 56) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 260, 8, p =>
			{
				p.Velocity *= .98f;
				p.Scale = 0.25f + 0.15f * (float)Math.Sin(p.TimeActive * 0.05f);

			}));
	}

	//Oganesson: like Xenon, but lasts much shorter, up faster, and splits into 2 smaller ones
	internal static void SpawnOganessonParticles(Player player)
	{
		var start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		var velocity = new Vector2(0f, Main.rand.NextFloat(-2f, -1f));

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(255, 255, 255) * 0.75f, Main.rand.NextFloat(0.25f, 0.45f), 60, 8, p =>
			{
				p.Velocity.Y *= Main.rand.NextFloat(.96f, .99f);
				if (p.TimeActive == 50)
				{
					for (int i = 0; i < 2; i++)
						ParticleHandler.SpawnParticle(new GlowParticle(p.Position, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)), new Color(255, 255, 255) * 0.25f, Main.rand.NextFloat(0.25f, 0.3f), 30, 8));
				}
			}));
	}
}