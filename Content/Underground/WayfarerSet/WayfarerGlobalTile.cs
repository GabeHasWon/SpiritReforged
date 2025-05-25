using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.WayfarerSet;

internal class WayfarerGlobalTile : GlobalTile
{
	public static readonly SoundStyle PositiveOutcome = new("SpiritReforged/Assets/SFX/Ambient/PositiveOutcome")
	{
		Pitch = -.35f
	};

	public static readonly HashSet<int> PotTypes = [TileID.Pots];

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		const int maxDistance = 800;

		if (WorldMethods.Generating || effectOnly || fail)
			return;

		var world = new Vector2(i, j).ToWorldCoordinates();
		var player = Main.player[Player.FindClosest(world, 16, 16)];

		if (ActiveAndInRange() && PotTypes.Contains(type))
		{
			if (!player.HasBuff<ExplorerPot>())
				DoFX(player);

			player.AddBuff(ModContent.BuffType<ExplorerPot>(), 600);
		}

		if (ActiveAndInRange() && Main.tileSpelunker[type] && Main.tileSolid[type])
		{
			if (!player.HasBuff<ExplorerMine>())
				DoFX(player);

			player.AddBuff(ModContent.BuffType<ExplorerMine>(), 600);
		}

		bool ActiveAndInRange() => WayfarerHead.SetActive(player) && player.DistanceSQ(world) < maxDistance * maxDistance;
	}

	private static void DoFX(Player player)
	{
		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 2f }, player.Center);
			SoundEngine.PlaySound(PositiveOutcome, player.Center);

			for (int i = 0; i < 12; i++)
				ParticleHandler.SpawnParticle(new GlowParticle(player.Center, Main.rand.NextVector2CircularEdge(1, 1), Color.PapayaWhip, Main.rand.NextFloat(0.25f, 0.4f), Main.rand.Next(30, 50), 8));
		}
	}
}