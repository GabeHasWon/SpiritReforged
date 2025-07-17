using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;
using static System.Net.Mime.MediaTypeNames;

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
	private float _sine;

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
		NPC.Opacity = NPC.IsABestiaryIconDummy ? 1 : 0;

		//NPC.HitSound = SoundID.NPCHit1; //Do sounds in HitEffect because it gives us more control
		//NPC.DeathSound = SoundID.NPCHit1 with { Pitch = 0.5f };
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

		if (Falling)
		{
			FallBehaviour();
			return;
		}

		NPC.Opacity = Math.Min(NPC.Opacity + 0.1f, 1);
		NPC.TargetClosest();

		if (ParentNPC != null)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && (ParentNPC.type != Type || !ParentNPC.active || ParentNPC.ModNPC is Stactus s && s.Falling)) //ParentNPC is invalid
			{
				if (Segment is SegmentType.Top && FindNewParent(out var p))
				{
					Parent = p.whoAmI;
					NPC.netUpdate = true;
				}
				else if (!Falling)
				{
					NPC.velocity += new Vector2(0, -Main.rand.NextFloat(3f, 5f)).RotatedByRandom(1);
					Falling = true;

					NPC.netUpdate = true;
				}
			}

			float sway = (float)Math.Sin((_sine + NPC.whoAmI * 25f) / 10f) * 4;
			var origin = ParentNPC.Center - new Vector2(sway * Math.Min((SpawnTime - SpawnTimeMax - 20) / 60f, 1), NPC.height);
			bool belowOrigin = NPC.Center.Y > origin.Y;

			NPC.Center = belowOrigin ? origin : NPC.Center;

			if (belowOrigin && NPC.velocity.Y > 0)
				NPC.velocity = Vector2.Zero;
		}
		else //This has no parent (the base of the stack)
		{
			var target = Main.player[NPC.target].Center;

			float speed = (NPC.Distance(target) < 16 * 18) ? Math.Sign(NPC.DirectionTo(target).X) * 0.2f : 0;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, speed, 0.05f);

			if (NPC.velocity.Y == 0 && Math.Abs(NPC.velocity.X) > 0.1f && Main.rand.NextBool(10))
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(NPC.Bottom, -NPC.velocity, Color.PaleGoldenrod, Main.rand.NextFloat(0.05f, 0.1f), Common.Easing.EaseFunction.EaseCubicIn, 60)
				{
					Pixellate = true,
					PixelDivisor = 2,
					TertiaryColor = Color.IndianRed
				});
			}

			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
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
				SpawnStack(NPC, Main.rand.Next(3, 6) + (Main.expertMode ? 1 : 0));

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
			NPC.velocity.Y *= -0.9f;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				//Take continuous damage in contact with the ground but don't display it
				var hit = NPC.CalculateHitInfo(20, 1, damageVariation: true) with { HideCombatText = true };
				NPC.StrikeNPC(hit);

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendStrikeNPC(NPC, hit);

				if (NPC.velocity.X == 0)
					NPC.velocity.X = Main.rand.NextFloat(-2f, 2f);

				NPC.velocity.X *= 1.1f;
				NPC.netUpdate = true;
			}
		}
	}

	private bool FindNewParent(out NPC npc)
	{
		npc = ParentNPC;
		while (npc != null && npc.ModNPC is Stactus s && (s.Falling || !npc.active))
		{
			npc = s.ParentNPC;
		}

		return npc != null;
	}

	private NPC GetBase()
	{
		var npc = ParentNPC;
		while (npc != null && npc.ModNPC is Stactus s && s.ParentNPC is NPC n)
		{
			npc = n;
		}

		return npc;
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

	public override void FindFrame(int frameHeight)
	{
		_sine++;
		NPC.frame.Y = frameHeight * Segment switch
		{
			SegmentType.Base => 3,
			SegmentType.Middle => NPC.whoAmI % 2 + 1,
			SegmentType.Top => 0,
			_ => 1
		};
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.OasisCactus, Scale: Main.rand.NextFloat(1f, 1.2f));

			if (!Falling)
				SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center);
		}

		if (NPC.life <= 0 && Segment is SegmentType.Top && GetBase() is NPC b && b.active)
			(b.ModNPC as Stactus).Falling = true;

		if (NPC.life > 0 || Main.expertMode && !Falling)
			return;

		if (!Main.dedServ)
		{
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

			SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.5f }, NPC.Center);
		}
	}

	public override bool CheckDead()
	{
		if (Main.expertMode && !Falling) //In expert mode, segments must be falling before they can die
		{
			NPC.life = NPC.lifeMax;
			Falling = true;

			return false;
		}

		return true;
	}

	#region on_hide
	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
	{
		if (SpawnTime < SpawnTimeMax)
			boundingBox = Rectangle.Empty; //Hide the bounding box when spawning in
	}

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => Falling ? false : null;

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => SpawnTime >= SpawnTimeMax;
	public override bool CanHitNPC(NPC target) => SpawnTime >= SpawnTimeMax;
	#endregion

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		var source = NPC.frame with { Height = NPC.frame.Height - 2 };
		var origin = new Vector2(source.Width / 2, source.Height / 2 + 6);
		var color = NPC.GetAlpha(drawColor);

		if (NPC.IsABestiaryIconDummy) //Draw the Bestiary entry
		{
			source = texture.Frame(1, Main.npcFrameCount[Type], 0, 3, sizeOffsetY: -2);
			int frameHeight = texture.Height / Main.npcFrameCount[Type];

			center.Y -= 6;

			Main.EntitySpriteDraw(texture, center, source, color, NPC.rotation, origin, NPC.scale, default);
			Main.EntitySpriteDraw(texture, center - new Vector2(GetSway(1), NPC.height), source with { Y = source.Y - frameHeight }, color, NPC.rotation, origin, NPC.scale, default);
			Main.EntitySpriteDraw(texture, center - new Vector2(GetSway(2), NPC.height * 2), source with { Y = source.Y - frameHeight * 3 }, color, NPC.rotation, origin, NPC.scale, default);

			source = Face.Frame(1, 2, 0, (int)(_sine % 300) / 285, sizeOffsetY: -2);
			Main.EntitySpriteDraw(Face.Value, center - new Vector2(GetSway(2), NPC.height * 2 - 4), source, color, NPC.rotation, source.Size() / 2, NPC.scale, default);

			return false;
		}

		Main.EntitySpriteDraw(texture, center, source, color, NPC.rotation, origin, NPC.scale, default);

		if (Segment is SegmentType.Top)
		{
			source = Face.Frame(1, 2, 0, (int)(NPC.localAI[0] % 300) / 285, sizeOffsetY: -2);
			float turn = Math.Clamp((Main.player[NPC.target].Center.X - NPC.Center.X) / 50f, -4, 4);

			Main.EntitySpriteDraw(Face.Value, center + new Vector2((int)turn, 4), source, color, NPC.rotation, source.Size() / 2, NPC.scale, default);
		}

		return false;

		float GetSway(int index) => (float)Math.Sin((_sine + index * 25f) / 10f) * 4;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(Falling);
	public override void ReceiveExtraAI(BinaryReader reader) => Falling = reader.ReadBoolean();
}