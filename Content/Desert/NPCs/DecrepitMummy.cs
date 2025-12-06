using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Desert.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.NPCs;

public class DecrepitMummy : ModNPC
{
	public override void SetStaticDefaults()
	{
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Velocity = 1 });
		Main.npcFrameCount[Type] = 15;
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(16, 32);
		NPC.lifeMax = 50;
		NPC.defense = 5;
		NPC.damage = 12;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0.8f;
		NPC.aiStyle = NPCAIStyleID.Fighter;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter = (NPC.frameCounter + 0.2f) % Main.npcFrameCount[Type];
		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			Vector2 velocity = NPC.velocity;

			for (int i = 0; i < 3; i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Bone, velocity.X, velocity.Y);

			if (hit.Damage > NPC.lifeMax / 8)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Mummy" + Main.rand.NextFromList(2, 3)).Type, 1f);

			if (NPC.life <= 0)
			{
				for (int i = 1; i < 4; i++)
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Mummy" + i).Type, 1f);

				for (int i = 0; i < 12; i++)
				{
					var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, velocity.X, -1, 100);
					dust.fadeIn = 2;
					dust.noGravity = true;
				}
			}
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame with { Height = NPC.frame.Height - 2 }; //Remove padding
		Vector2 position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}
}