using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

[AutoloadBanner]
public class StactusCrimson : Stactus
{
	public override void OnSpawn(IEntitySource source) { }

	public override void SetDefaults()
	{
		base.SetDefaults();
		Params = new(DustID.CrimsonPlants, 100);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "CrimsonDesert");

	public override void FallBehaviour()
	{
		if (NPC.collideX || NPC.collideY)
		{
			SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.75f }, NPC.Center);

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				var hit = NPC.CalculateHitInfo(9999, 1, damageVariation: true) with { HideCombatText = true };
				NPC.StrikeNPC(hit);

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendStrikeNPC(NPC, hit);

				NPC.netUpdate = true;

				for (int i = 0; i < 5; i++)
					Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center - NPC.velocity, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1, 2.2f), ModContent.ProjectileType<CactusSpine>(), NPC.damage, 1);
			}
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!spawnInfo.Player.ZoneDesert || !spawnInfo.Player.ZoneCrimson || spawnInfo.SpawnTileType != TileID.Crimsand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(Falling);
	public override void ReceiveExtraAI(BinaryReader reader) => Falling = reader.ReadBoolean();
}