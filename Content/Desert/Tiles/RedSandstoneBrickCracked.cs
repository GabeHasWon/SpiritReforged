using SpiritReforged.Common.Particle;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class RedSandstoneBrickCracked : RedSandstoneBrick
{
	private class DustStream : Particle
	{
		public DustStream(Vector2 position)
		{
			Position = position;
			MaxTime = 90;
		}

		public override void Update()
		{
			if (TimeActive == 1)
				SoundEngine.PlaySound(Main.rand.Next([DebrisQuiet with { Volume = 0.8f, PitchRange = (0.5f, 1f) }, DebrisLoud with { Volume = 0.1f, Pitch = 1f, PitchVariance = 0.4f }]), Position);

			Rectangle area = new((int)Position.X - 8, (int)Position.Y - 8, 16, 2);
			if (Main.rand.NextBool(10))
			{
				var d = Dust.NewDustDirect(area.TopLeft(), area.Width, area.Height, DustID.DynastyShingle_Red, Scale: Main.rand.NextFloat(0.5f, 1));
				d.noGravity = true;
				d.velocity = new(0, 3);
			}

			if (Main.rand.NextBool(20))
			{
				var g = Gore.NewGoreDirect(new EntitySource_Misc("Particle"), Main.rand.NextVector2FromRectangle(area), Vector2.Zero, SpiritReforgedMod.Instance.Find<ModGore>("RedBrick" + Main.rand.Next(1, 6)).Type);
				g.scale = Main.rand.NextFloat(0.5f, 1);
				g.velocity = Vector2.Zero;
			}
		}
	}

	public static readonly SoundStyle DebrisQuiet = new("SpiritReforged/Assets/SFX/Ambient/FallingDebris1")
	{
		PitchVariance = 0.5f,
		MaxInstances = 3
	};

	public static readonly SoundStyle DebrisLoud = new("SpiritReforged/Assets/SFX/Ambient/FallingDebris2");

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.gamePaused && Main.GameUpdateCount % 10 < 2 && Main.rand.NextBool(550) && Main.LocalPlayer.velocity.X != 0 && !WorldGen.SolidOrSlopedTile(i, j + 1))
			ParticleHandler.SpawnParticle(new DustStream(new Vector2(i, j).ToWorldCoordinates(8, 16)));
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail)
		{
			var source = new EntitySource_TileBreak(i, j);
			var center = new Vector2(i, j) * 16;

			for (int x = 1; x < 6; x++)
			{
				if (Main.rand.NextBool())
					Gore.NewGore(source, center, Vector2.Zero, Mod.Find<ModGore>("RedBrick" + x).Type);
			}

			SoundEngine.PlaySound(DebrisQuiet, center + new Vector2(8));
		}
		else if (effectOnly && Main.rand.NextBool(4))
		{
			ParticleHandler.SpawnParticle(new DustStream(new Vector2(i, j).ToWorldCoordinates(8, 16)));
		}
	}
}