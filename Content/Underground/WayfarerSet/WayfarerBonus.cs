﻿using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.WayfarerSet;

internal class WayfarerBonus : ILoadable
{
	public static readonly SoundStyle PositiveOutcome = new("SpiritReforged/Assets/SFX/Ambient/PositiveOutcome")
	{
		Pitch = -.35f
	};

	public static readonly HashSet<int> PotTypes = [TileID.Pots];

	public void Load(Mod mod) => TileEvents.OnKillTile += KillTile;
	public void Unload() { }

	private static void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly)
	{
		const int maxDistance = 800;

		if (WorldMethods.Generating || effectOnly || fail)
			return;

		var world = new Vector2(i, j).ToWorldCoordinates();
		var player = Main.player[Player.FindClosest(world, 16, 16)];

		if (WayfarerHead.SetActive(player) && player.DistanceSQ(world) < maxDistance * maxDistance)
		{
			if (PotTypes.Contains(type))
				GrantBuffCommon(player, ModContent.BuffType<ExplorerPot>());

			if (Main.tileSpelunker[type] && Main.tileSolid[type])
				GrantBuffCommon(player, ModContent.BuffType<ExplorerMine>());
		}
	}

	private static void GrantBuffCommon(Player player, int buffType)
	{
		if (!player.HasBuff(buffType) && !Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 2f }, player.Center);
			SoundEngine.PlaySound(PositiveOutcome, player.Center);

			for (int i = 0; i < 12; i++)
				ParticleHandler.SpawnParticle(new GlowParticle(player.Center, Main.rand.NextVector2CircularEdge(1, 1), Color.PapayaWhip, Main.rand.NextFloat(0.25f, 0.4f), Main.rand.Next(30, 50), 8));
		}

		player.AddBuff(buffType, 600);
	}
}