using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.WayfarerSet;

internal class WayfarerPlayer : ModPlayer
{
	public bool active = false;
	public int miningStacks = 1;
	public int movementStacks = 1;

	public override void ResetEffects()
	{
		active = false;

		if (Player.FindBuffIndex(ModContent.BuffType<ExplorerMine>()) < 0)
			miningStacks = 1;

		if (Player.FindBuffIndex(ModContent.BuffType<ExplorerPot>()) < 0)
			movementStacks = 1;
	}

	public override void PostUpdateEquips()
	{
		if (active)
			Player.GetModPlayer<CoinLootPlayer>().enemyCoinMultiplier = 1.1f;
	}
}

internal class WayfarerGlobalTile : GlobalTile
{
	private static readonly int[] PotTypes = [TileID.Pots, ModContent.TileType<BiomePots>(), ModContent.TileType<StackablePots>(), ModContent.TileType<MushroomPots>()];

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (Main.dedServ)
			return;

		var player = Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(), 16, 16)];

		if (player.GetModPlayer<WayfarerPlayer>().active && PotTypes.Contains(type))
		{
			if (!player.HasBuff<ExplorerPot>())
				DoFX(player);

			player.AddBuff(ModContent.BuffType<ExplorerPot>(), 600);
		}

		if (player.GetModPlayer<WayfarerPlayer>().active && Main.tileSpelunker[type] && Main.tileSolid[type])
		{
			if (!player.HasBuff<ExplorerMine>())
				DoFX(player);

			player.AddBuff(ModContent.BuffType<ExplorerMine>(), 600);
		}
	}

	private static void DoFX(Player player)
	{
		SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 2f }, player.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/PositiveOutcome") with { Pitch = -.35f }, player.Center);

		for (int i = 0; i < 12; i++)
			ParticleHandler.SpawnParticle(new GlowParticle(player.Center, Main.rand.NextVector2CircularEdge(1, 1), Color.PapayaWhip, Main.rand.NextFloat(0.25f, 0.4f), Main.rand.Next(30, 50), 8));
	}
}