using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs;

[AutoloadBanner]
internal class Stactus : ModNPC
{
	public const int SpawnTimeMax = 120;
	private static readonly Asset<Texture2D> Face = DrawHelpers.RequestLocal(typeof(Stactus), "Stactus_Face", false);

	public enum SegmentType : byte
	{
		Base,
		Middle,
		Top
	}

	public NPC ParentNPC => Parent > -1 ? Main.npc[Parent] : null;

	/// <summary> The index of NPC this one is stacked atop. </summary>
	public int Parent
	{
		get => (int)NPC.ai[0];
		set => NPC.ai[0] = value;
	}

	public SegmentType Segment
	{
		get => (SegmentType)NPC.ai[1];
		set => NPC.ai[1] = (int)value;
	}

	public int SpawnTime
	{
		get => (int)NPC.ai[2];
		set => NPC.ai[2] = value;
	}

	public bool Falling { get; private set; }

	/// <summary> Spawns a stack of Stactus at <paramref name="fromNPC"/>, representing the first in the stack. </summary>
	public static void SpawnStack(NPC fromNPC, int height)
	{
		int parent = fromNPC.whoAmI;

		for (int i = 1; i < height; i++)
		{
			//The only segment that can't be calculated locally after spawn is SegmentType.Top, so do it here for continuity
			var segment = (i == 0) ? SegmentType.Base : ((i == height - 1) ? SegmentType.Top : SegmentType.Middle);
			var source = (parent != -1) ? Main.npc[parent].GetSource_FromThis() : new EntitySource_SpawnNPC();

			var npc = NPC.NewNPCDirect(source, fromNPC.Center, ModContent.NPCType<Stactus>(), 0, parent, (int)segment, SpawnTimeMax);
			npc.velocity.Y = (i + 1) * -1.5f;

			parent = npc.whoAmI;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers() { Position = new Vector2(0, 12) };
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(28);
		NPC.knockBackResist = 0;
		NPC.aiStyle = -1;
		NPC.lifeMax = 70;
		NPC.damage = 10;
		NPC.defense = 4;
		NPC.value = Item.buyPrice(silver: 1, copper: 50);
		NPC.Opacity = 0;

		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCHit1 with { Pitch = 0.5f };
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void AI()
	{
		if (++SpawnTime < SpawnTimeMax)
		{
			NPC.dontTakeDamage = true;
			SpawnBehaviour();

			return;
		}
		else
		{
			NPC.dontTakeDamage = false;
		}

		NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
		NPC.TargetClosest();

		if (ParentNPC != null)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && (ParentNPC.type != Type || !ParentNPC.active || ParentNPC.ModNPC is Stactus s && s.Falling)) //ParentNPC is invalid
			{
				if (!Falling)
				{
					NPC.velocity += new Vector2(0, -Main.rand.NextFloat(3f, 5f)).RotatedByRandom(1);
					Falling = true;

					NPC.netUpdate = true;
				}
			}

			if (Falling)
			{
				FallBehaviour();
				return;
			}

			float sway = (float)Math.Sin((++NPC.localAI[0] + NPC.whoAmI * 20) / 30f) * 2;
			var origin = ParentNPC.Center - new Vector2(sway * Math.Min((SpawnTime - SpawnTimeMax - 20) / 60f, 1), NPC.height);
			bool belowOrigin = NPC.Center.Y > origin.Y;

			NPC.Center = belowOrigin ? origin : NPC.Center;

			if (belowOrigin && NPC.velocity.Y > 0)
				NPC.velocity = Vector2.Zero;
		}
		else //This has no parent (the base of the stack)
		{

		}

		NPC.behindTiles = Segment != SegmentType.Top;
	}

	private void SpawnBehaviour()
	{
		Parent = -1;

		if (!Main.dedServ)
		{
			Dust.NewDustDirect(NPC.BottomLeft, NPC.width, 2, DustID.Sand, 0, -2).noGravity = Main.rand.NextBool();

			if (SpawnTime > SpawnTimeMax / 3 && Main.rand.NextBool(15))
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, Color.PaleGoldenrod, SpawnTime / (float)SpawnTimeMax * 0.17f, Common.Easing.EaseFunction.EaseCubicIn, 90)
				{
					Pixellate = true,
					PixelDivisor = 4,
					TertiaryColor = Color.SaddleBrown
				});
			}
		}

		if (SpawnTime == SpawnTimeMax - 1)
		{
			if (!Main.dedServ)
			{
				for (int i = 0; i < 2; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, Color.PaleGoldenrod, 0.3f, Common.Easing.EaseFunction.EaseCircularOut, Main.rand.Next(100, 180))
					{
						Pixellate = true,
						PixelDivisor = 4,
						TertiaryColor = Color.SaddleBrown
					});
				}

				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, (Vector2.UnitY * -2f).RotatedByRandom(0.4f), Color.PaleGoldenrod, 0.2f, Common.Easing.EaseFunction.EaseCubicIn, 90)
				{
					Pixellate = true,
					PixelDivisor = 4,
					TertiaryColor = Color.IndianRed
				});

				for (int i = 0; i < 4; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, (Vector2.UnitY * -Main.rand.NextFloat(5f)).RotatedByRandom(0.5f), Color.PaleGoldenrod, Main.rand.NextFloat(0.1f, 0.17f), Common.Easing.EaseFunction.EaseCubicIn, 60)
					{
						Pixellate = true,
						PixelDivisor = 4,
						TertiaryColor = Color.IndianRed
					});
				}

				for (int i = 0; i < 15; i++)
					Dust.NewDustDirect(NPC.BottomLeft, NPC.width, 2, DustID.Sand, 0, -5).noGravity = Main.rand.NextBool();
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				SpawnStack(NPC, Main.expertMode ? Main.rand.Next(3, 9) : Main.rand.Next(3, 6));

				NPC.velocity.Y = -3;
				NPC.netUpdate = true;
			}
		}
	}

	private void FallBehaviour()
	{
		NPC.rotation += NPC.velocity.X * 0.05f;

		if (NPC.collideX || NPC.collideY)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.StrikeInstantKill();

			SoundEngine.PlaySound(SoundID.NPCDeath1 with { PitchVariance = 0.3f, Volume = 0.5f }, NPC.Center);
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (spawnInfo.PlayerInTown || spawnInfo.SpawnTileType != TileID.Sand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;
	public override int SpawnNPC(int tileX, int tileY)
	{
		var spawn = new Vector2(tileX, tileY).ToWorldCoordinates();
		var p = Main.player[Player.FindClosest(spawn, 8, 8)];

		for (int a = 0; a < 30; a++) //Try to spawn closer to players than NPCs normally do
		{
			var position = (p.Center / 16 + Vector2.UnitX * Main.rand.Next(10, 40)).ToPoint16();
			int i = position.X;
			int j = position.Y;

			WorldMethods.FindGround(position.X, ref j);

			var t = Main.tile[i, j];

			if (t.HasTile && t.TileType == TileID.Sand)
				return NPC.NewNPC(new EntitySource_SpawnNPC(), i * 16, j * 16, Type, 0, -1);
		}

		return NPC.NewNPC(new EntitySource_SpawnNPC(), tileX * 16, tileY * 16, Type, 0, -1);
	}

	public override void FindFrame(int frameHeight) => NPC.frame.Y = frameHeight * Segment switch
	{
		SegmentType.Base => 3,
		SegmentType.Middle => NPC.whoAmI % 2 + 1,
		SegmentType.Top => 0,
		_ => 1
	};

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 3; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.OasisCactus, Scale: Main.rand.NextFloat(1f, 1.2f));

		if (NPC.life > 0)
			return;

		if (Segment is SegmentType.Top)
		{
			for (int i = 1; i < 8; i++)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("StactusTop" + i).Type);
		}
		else
		{
			for (int i = 0; i < 2; i++)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Stactus" + Main.rand.Next(1, 6)).Type);
		}

		if (Main.rand.NextBool())
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Stactus6").Type);
	}

	#region on_hide
	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
	{
		if (SpawnTime < SpawnTimeMax)
			boundingBox = Rectangle.Empty; //Hide the bounding box when spawning in
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => SpawnTime >= SpawnTimeMax;
	public override bool CanHitNPC(NPC target) => SpawnTime >= SpawnTimeMax;
	#endregion

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		var source = NPC.frame with { Height = NPC.frame.Height - 2 };
		Main.EntitySpriteDraw(TextureAssets.Npc[Type].Value, center, source, NPC.GetAlpha(drawColor), NPC.rotation, new Vector2(source.Width / 2, source.Height / 2 + 6), NPC.scale, default);

		if (Segment is SegmentType.Top)
		{
			source = Face.Frame(1, 2, 0, (int)(NPC.localAI[0] % 300) / 285, sizeOffsetY: -2);
			float turn = Math.Clamp((Main.player[NPC.target].Center.X - NPC.Center.X) / 100f, -4, 4);

			Main.EntitySpriteDraw(Face.Value, center + new Vector2((int)turn, 4), source, NPC.GetAlpha(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, default);
		}

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(Falling);
	public override void ReceiveExtraAI(BinaryReader reader) => Falling = reader.ReadBoolean();
}