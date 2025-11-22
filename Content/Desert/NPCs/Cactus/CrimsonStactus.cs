using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

[AutoloadBanner]
public class CrimsonStactus : Stactus
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		parameters = new(DustID.CrimsonPlants, 100, ["Juvenile", "PricklyPears", "WreathOfStalks"]);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "CrimsonDesert");

	public override void FallBehaviour()
	{
		if (!NPC.collideX && !NPC.collideY)
			return;

		SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.75f }, NPC.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var hit = NPC.CalculateHitInfo(9999, 1, damageVariation: true) with { HideCombatText = true };
			NPC.StrikeNPC(hit);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendStrikeNPC(NPC, hit);

			NPC.netUpdate = true;

			for (int i = 0; i < 5; i++)
				Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center - NPC.velocity, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1, 2.2f), ModContent.ProjectileType<CactusSpine>(), NPC.damage, 1, ai0: Main.rand.Next(0, 3));
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!spawnInfo.Player.ZoneDesert || !spawnInfo.Player.ZoneCrimson || spawnInfo.SpawnTileType != TileID.Crimsand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;
}