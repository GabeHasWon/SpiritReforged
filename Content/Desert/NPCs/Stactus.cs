using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs;

[AutoloadBanner]
internal class Stactus : ModNPC
{
	public const int SpawnTime = 120;

	private static readonly Asset<Texture2D> Top = DrawHelpers.RequestLocal(typeof(Stactus), "Stactus_Top", false);
	private static readonly Asset<Texture2D> Face = DrawHelpers.RequestLocal(typeof(Stactus), "Stactus_Face", false);

	public enum SegmentType
	{
		Base,
		Middle,
		Top
	}

	public NPC ParentNPC => Parent > -1 ? Main.npc[Parent] : null;
	public bool HasParent => ParentNPC != null;
	public bool SpawningIn => _spawnTime < SpawnTime;

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

	public bool Falling { get; private set; }

	private Vector2 _easedVelocity;
	private int _spawnTime;

	/// <summary> Spawns a stack of Stactus at the given position, optionally from <paramref name="fromNPC"/>, representing the first in the stack. </summary>
	public static void SpawnStack(Vector2 position, int height, NPC fromNPC = null)
	{
		int parent = -1;
		int start = 0;

		if (fromNPC != null)
		{
			parent = fromNPC.whoAmI;
			start++;
		}

		for (int i = start; i < height; i++)
		{
			//The only segment that can't be calculated locally after spawn is SegmentType.Top, so do it here for continuity
			var segment = (i == 0) ? SegmentType.Base : ((i == height - 1) ? SegmentType.Top : SegmentType.Middle);
			var source = (parent != -1) ? Main.npc[parent].GetSource_FromThis() : NPC.GetSource_NaturalSpawn();

			var npc = NPC.NewNPCDirect(source, position, ModContent.NPCType<Stactus>(), 0, parent, (int)segment);
			parent = npc.whoAmI;
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
	public override void OnSpawn(IEntitySource source)
	{
		if (source is not EntitySource_Parent)
		{
			Parent = -1;
			SpawnStack(NPC.Center, Main.rand.Next(3, 6), NPC);

			NPC.netUpdate = true;
		}
	}

	public override void AI()
	{
		if (++_spawnTime < SpawnTime)
		{
			SpawnBehaviour();
			return;
		}

		NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
		NPC.TargetClosest();

		if (HasParent)
		{
			if (ParentNPC.type != Type || !ParentNPC.active || ParentNPC.ModNPC is Stactus s && s.Falling) //ParentNPC is invalid
			{
				if (!Falling)
				{
					NPC.netUpdate = true;
					NPC.velocity += new Vector2(0, -Main.rand.NextFloat(3f, 5f)).RotatedByRandom(1);

					Falling = true;
				}

				FallBehaviour();
				return;
			}

			float sway = (float)Math.Sin((++NPC.localAI[0] + NPC.whoAmI * 20) / 30f) * 2;
			var origin = ParentNPC.Center - new Vector2(sway * Math.Min((_spawnTime - SpawnTime - 20) / 60f, 1), NPC.height);
			bool belowOrigin = NPC.Center.Y > origin.Y;

			NPC.Center = belowOrigin ? origin : NPC.Center;

			if (belowOrigin && NPC.velocity.Y > 0)
				NPC.velocity = Vector2.Zero;
		}
		else //This has no parent (the base of the stack)
		{

		}

		NPC.behindTiles = Segment != SegmentType.Top;
		NPC.dontCountMe = HasParent;
		_easedVelocity = Vector2.Lerp(_easedVelocity, NPC.velocity, 0.8f);
	}

	private void SpawnBehaviour()
	{
		bool isBase = Segment is SegmentType.Base; //Ensures visuals are only spawned from one segment to make them easier to control
		if (isBase)
		{
			Dust.NewDustDirect(NPC.BottomLeft, NPC.width, 2, DustID.Sand, 0, -2).noGravity = Main.rand.NextBool();

			if (_spawnTime > SpawnTime / 3 && Main.rand.NextBool(15))
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, Color.PaleGoldenrod, _spawnTime / (float)SpawnTime * 0.17f, Common.Easing.EaseFunction.EaseCubicIn, 90)
				{
					Pixellate = true,
					PixelDivisor = 5,
					TertiaryColor = Color.SaddleBrown
				});
			}
		}

		if (_spawnTime == SpawnTime - 1)
		{
			if (isBase)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -Vector2.UnitY, Color.PaleGoldenrod, 0.3f, Common.Easing.EaseFunction.EaseCircularOut, 120)
				{
					Pixellate = true,
					PixelDivisor = 5,
					TertiaryColor = Color.SaddleBrown
				});

				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, Vector2.UnitY * -2f, Color.PaleGoldenrod, 0.2f, Common.Easing.EaseFunction.EaseCubicIn, 90)
				{
					Pixellate = true,
					PixelDivisor = 5,
					TertiaryColor = Color.IndianRed
				});

				for (int i = 0; i < 4; i++)
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, (Vector2.UnitY * -Main.rand.NextFloat(5f)).RotatedByRandom(0.5f), Color.PaleGoldenrod, Main.rand.NextFloat(0.1f, 0.17f), Common.Easing.EaseFunction.EaseCubicIn, 60)
					{
						Pixellate = true,
						PixelDivisor = 5,
						TertiaryColor = Color.IndianRed
					});
				}
			}

			NPC.velocity.Y = -Main.rand.NextFloat(5f, 8f);
			NPC.netUpdate = true;
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
		for (int i = 0; i < 3; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.OasisCactus, Scale: Main.rand.NextFloat(1f, 1.2f));

		if (NPC.life > 0 || Main.dedServ)
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

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		bool isTop = Segment is SegmentType.Top;

		if (isTop)
			DrawTopFrames(NPC.Center - new Vector2(0, 4) - screenPos, NPC.GetAlpha(drawColor), 0);

		var center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		var source = NPC.frame with { Height = NPC.frame.Height - 2 };
		Main.EntitySpriteDraw(TextureAssets.Npc[Type].Value, center, source, NPC.GetAlpha(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, default);

		if (isTop)
		{
			DrawTopFrames(center - new Vector2(0, 4), NPC.GetAlpha(drawColor), 3, 2, 1);

			source = Face.Frame(1, 2, 0, (int)(NPC.localAI[0] % 300) / 285, sizeOffsetY: -2);
			float turn = Math.Clamp((Main.player[NPC.target].Center.X - NPC.Center.X) / 100f, -4, 4);

			Main.EntitySpriteDraw(Face.Value, center + new Vector2((int)turn, 4), source, NPC.GetAlpha(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, default);
		}

		return false;
	}

	private void DrawTopFrames(Vector2 position, Color drawColor, params int[] frames)
	{
		float rotation = -_easedVelocity.X * 0.02f;

		foreach (int frame in frames)
		{
			var source = Top.Value.Frame(1, 4, 0, frame, 0, -2);
			Main.EntitySpriteDraw(Top.Value, position, source, NPC.GetAlpha(drawColor), NPC.rotation + rotation, new Vector2(source.Width / 2, source.Height), NPC.scale, default);
		}
	}
}