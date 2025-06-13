using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Underground.NPCs.MossSlimes;

/// <summary>
/// Prototype class used as a parent class for all Moss Slimes, and also for the bestiary entry.
/// </summary>
[AutoloadBanner]
internal class MossSlime : ModNPC
{
	protected static Dictionary<int, Asset<Texture2D>> FrontSpritesById = [];
	protected static Dictionary<int, Asset<Texture2D>> BackSpritesById = [];
	protected static Dictionary<int, HashSet<int>> SpawnTilesById = [];

	private static int DummyBestiaryType = -1;

	public override string Texture => "SpiritReforged/Content/Underground/NPCs/MossSlimes/MossSlimeBase";

	protected virtual Vector3 LightColor { get; }
	protected virtual int DustType { get; }
	protected virtual int MossType { get; }
	protected virtual HashSet<int> TileTypes { get; } = [];

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;

		if (Type != ModContent.NPCType<MossSlime>() && !Main.dedServ)
		{
			string path = $"SpiritReforged/Content/Underground/NPCs/MossSlimes/{Name}";
			FrontSpritesById.Add(Type, ModContent.Request<Texture2D>(path + "_Front"));
			BackSpritesById.Add(Type, ModContent.Request<Texture2D>(path + "_Back"));
		}

		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.ShimmerSlime;

		SpawnTilesById.Add(Type, TileTypes);

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Slime);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.BlackSlime);
		NPC.aiStyle = NPCAIStyleID.Slime;

		Banner = ModContent.NPCType<MossSlime>();

		AIType = NPCID.BlueSlime;
		AnimationType = NPCID.BlueSlime;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		// Give all credit to Moss Slime as the "prototypical" slime
		ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type] = ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[ModContent.NPCType<MossSlime>()];

		if (Type != ModContent.NPCType<MossSlime>())
			return;

		bestiaryEntry.AddInfo(this, "Caverns");
	}

	public override bool PreAI()
	{
		Lighting.AddLight(NPC.Center, LightColor * .75f);
		return true;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.AddCommon(ItemID.Gel, 1, 1, 3);
		npcLoot.AddCommon(ItemID.SlimeStaff, 10000);
		npcLoot.AddCommon(MossType, 1, 5, 10);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (GetType() == typeof(MossSlime))
			return 0;

		return spawnInfo.SpawnTileY > Main.worldSurface && SpawnTilesById[Type].Contains(spawnInfo.SpawnTileType) ? 0.3f : 0;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Type == ModContent.NPCType<MossSlime>())
		{
			DummyBestiaryType = (Main.GameUpdateCount % 480) switch
			{
				< 60 => ModContent.NPCType<KryptonMossSlime>(),
				< 120 => ModContent.NPCType<NeonMossSlime>(),
				< 180 => ModContent.NPCType<XenonMossSlime>(),
				< 240 => ModContent.NPCType<ArgonMossSlime>(),
				< 300 => ModContent.NPCType<OganessonMossSlime>(),
				< 360 => ModContent.NPCType<RadonMossSlime>(),
				< 420 => ModContent.NPCType<HeliumMossSlime>(),
				_ => ModContent.NPCType<LavaMossSlime>()
			};

			NPC.type = DummyBestiaryType;
			DrawMoss(spriteBatch, screenPos, drawColor, true);
			return NPC.IsABestiaryIconDummy;
		}

		DrawMoss(spriteBatch, screenPos, drawColor, true);
		return true;
	}

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (Type == ModContent.NPCType<MossSlime>() || DummyBestiaryType != -1)
		{
			DrawMoss(spriteBatch, screenPos, drawColor, false);
			NPC.type = ModContent.NPCType<MossSlime>();
			DummyBestiaryType = -1;
			return;
		}

		DrawMoss(spriteBatch, screenPos, drawColor, false);
	}

	private void DrawMoss(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, bool drawBack)
	{
		if (NPC.type == ModContent.NPCType<HeliumMossSlime>())
			drawColor = Main.DiscoColor;
		else
			drawColor = Color.White;

		Texture2D tex = (drawBack ? BackSpritesById[Type] : FrontSpritesById[Type]).Value;
		Vector2 position = NPC.Center - screenPos;

		spriteBatch.Draw(tex, position, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, NPC.scale, SpriteEffects.None, 0);

		if (drawBack)
		{
			var slime = ModContent.GetModNPC(Type) as MossSlime;
			int heightOff = NPC.frame.Y == 0 ? 0 : 2;
			Main.DrawItemIcon(spriteBatch, ContentSamples.ItemsByType[slime.MossType], position - new Vector2(0, heightOff), drawColor * 0.5f, 32);
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			bool dead = NPC.life <= 0;

			for (int k = 0; k < 5; k++)
			{
				int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType, 1.5f * hit.HitDirection, -2.5f, 0, default, Main.rand.NextFloat(0.4f, 0.8f));

				if (NPC.type == ModContent.NPCType<HeliumMossSlime>())
					Main.dust[dust].noGravity = true;				

				if (NPC.type == ModContent.NPCType<LavaMossSlime>() || NPC.type == ModContent.NPCType<OganessonMossSlime>() || NPC.type == ModContent.NPCType<RadonMossSlime>())
					Main.dust[dust].noLight = true;	
			}

			for (int k = 0; k < 8; k++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TintableDust, 2.5f * hit.HitDirection, -2.5f, 0, Color.Black * 0.5f, Main.rand.NextFloat(0.3f, 0.6f));

			if (dead)
			{
				for (int k = 0; k < 7; k++)
				{
					var start = NPC.BottomLeft + new Vector2(Main.rand.Next(NPC.width), 0);
					var velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -2).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.4f, 1f);

					ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
						new Color(LightColor) * 0.85f, Main.rand.NextFloat(0.25f, 0.45f), 40, 8, p =>
						{
							p.Velocity.X *= Main.rand.NextFloat(.8f, .9f);
							p.Velocity.Y *= Main.rand.NextFloat(.96f, .99f);
						}));
				}
			}
		}
	}
}
