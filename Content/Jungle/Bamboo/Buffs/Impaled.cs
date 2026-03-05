using Terraria.Audio;
using static SpiritReforged.Common.NPCCommon.SlowdownGlobalNPC;

namespace SpiritReforged.Content.Jungle.Bamboo.Buffs;

public class Impaled : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		var tile = Framing.GetTileSafely(player.Bottom - new Vector2(8));
		player.lifeRegenTime = 0;

		if (tile.HasTile && tile.TileType == ModContent.TileType<Tiles.BambooPike>())
		{
			if (player.buffTime[buffIndex] % 30 == 25)
				SoundEngine.PlaySound(SoundID.NPCHit1 with { PitchVariance = .2f, Pitch = -.5f }, player.Center);

			player.lifeRegen = Math.Min(player.lifeRegen, 0) - 15;
			player.velocity = new Vector2(0, .25f);
			player.gravity = 0f;
		}
		else
			player.velocity = new Vector2(player.velocity.X * .8f, player.velocity.Y > 0 ? player.velocity.Y : player.velocity.Y * .8f);

		if (Main.rand.NextBool(7))
		{
			Vector2 position = player.position + new Vector2(player.width * Main.rand.NextFloat(), player.height * Main.rand.NextFloat());
			Dust.NewDustPerfect(position, DustID.Blood, Vector2.Zero, 0, default, Main.rand.NextFloat(0.5f, 1.2f));
		}
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (npc.knockBackResist <= 0f)
			return;

		Tile tile = Framing.GetTileSafely(npc.Bottom - new Vector2(8));

		if (tile.HasTile && tile.TileType == ModContent.TileType<Tiles.BambooPike>()) //Severe effects
		{
			if (npc.buffTime[buffIndex] % 30 == 0)
				SoundEngine.PlaySound(SoundID.NPCHit1 with { PitchVariance = .2f, Pitch = -.5f }, npc.Center);

			ApplySlow(npc, 1);

			npc.lifeRegen = -10;
			npc.velocity = Vector2.UnitY * .1f;
		}
		else
		{
			ApplySlow(npc, .5f);
			npc.lifeRegen = 0;
		}

		if (Main.rand.NextBool(7))
		{
			Vector2 position = npc.position + new Vector2(npc.width * Main.rand.NextFloat(), npc.height * Main.rand.NextFloat());
			Dust.NewDustPerfect(position, DustID.Blood, Vector2.Zero, 0, default, Main.rand.NextFloat(0.5f, 1.2f));
		}
	}
}