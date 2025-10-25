using SpiritReforged.Common.Easing;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

[AutoloadBanner]
public class Stactus : ModNPC, IDeathCount
{
	public readonly record struct Parameters(int DustType, int SpawnTime);

	public static readonly HashSet<int> SandyTypes = [TileID.Sand, TileID.Sandstone, TileID.HardenedSand, TileID.SandstoneBrick, TileID.SandStoneSlab];
	private static readonly Dictionary<int, Asset<Texture2D>> Faces = [];

	public enum SegmentType : byte
	{
		Base,
		Middle,
		Top
	}

	public NPC ParentNPC => (Parent > -1) ? Main.npc[Parent] : null;

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

	public static readonly SoundStyle Death = new("SpiritReforged/Assets/SFX/NPCDeath/Squish")
	{
		Volume = 0.75f,
		Pitch = 0.6f,
		PitchVariance = 0.3f,
		MaxInstances = 0
	};

	public bool Falling { get; protected set; }
	public Parameters Params { get; protected set; }

	private byte _style;
	private float _sine;
	private bool _collisionSoundPlayed = false;

	/// <summary> Spawns a stack of Stactus at <paramref name="fromNPC"/>, representing the first in the stack. </summary>
	public void SpawnStack(int height)
	{
		int parent = NPC.whoAmI;

		for (int i = 1; i < height; i++)
		{
			//The only segment that can't be calculated locally after spawn is SegmentType.Top, so do it all here for continuity
			SegmentType segment = (i == 0) ? SegmentType.Base : ((i == height - 1) ? SegmentType.Top : SegmentType.Middle);
			var source = (parent != -1) ? Main.npc[parent].GetSource_FromThis() : new EntitySource_SpawnNPC();

			var npc = NPC.NewNPCDirect(source, NPC.Center, NPC.type, 0, parent, (int)segment, Params.SpawnTime);
			npc.velocity.Y = (i + 1) * -1.5f;

			parent = npc.whoAmI;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}
	}

	/// <summary> The face texture associated with this NPC type. </summary>
	public Texture2D FaceTexture => Faces[Type].Value;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 4;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers() { Position = new Vector2(0, 12) };
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

		Faces.Add(Type, DrawHelpers.RequestLocal(typeof(Stactus), Name + "_Face", false));
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(28);
		NPC.knockBackResist = 0;
		NPC.aiStyle = -1;
		NPC.lifeMax = 70;
		NPC.damage = 18;
		NPC.defense = 4;
		NPC.value = 80;
		NPC.Opacity = NPC.IsABestiaryIconDummy ? 1 : 0;
		NPC.dontCountMe = true;

		Params = new(DustID.OasisCactus, 120);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void OnSpawn(IEntitySource source)
	{
		if (Segment == SegmentType.Top)
			_style = (byte)Main.rand.Next(6); //Select a random style
	}

	public override void AI()
	{
		if (++SpawnTime < Params.SpawnTime)
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
			var origin = ParentNPC.Center - new Vector2(sway * Math.Clamp((SpawnTime - Params.SpawnTime - 50) / 30f, 0, 1), NPC.height);
			bool belowOrigin = NPC.Center.Y > origin.Y;

			NPC.Center = belowOrigin ? origin : NPC.Center;

			if (belowOrigin && NPC.velocity.Y > 0)
				NPC.velocity = Vector2.Zero;
		}
		else //This has no parent (the base of the stack)
		{
			var target = Main.player[NPC.target].Center;
			float maxSpeed = Main.zenithWorld ? 2 : 0.2f;
			float speed = (NPC.Distance(target) < 16 * 18) ? Math.Sign(NPC.DirectionTo(target).X) * maxSpeed : 0;

			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, speed, 0.05f);

			if (Math.Abs(NPC.velocity.X) > 0.1f && Main.rand.NextBool(10) && SandyTypes.Contains(GetSurfaceTile().TileType))
				SpawnSmoke(NPC.Bottom, -NPC.velocity, Main.rand.NextFloat(0.05f, 0.1f), 60, EaseFunction.EaseCubicIn, GetSurfaceTile());

			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
		}

		NPC.behindTiles = Segment != SegmentType.Top;
	}

	public virtual void SpawnBehaviour()
	{
		Parent = -1;

		if (!Main.dedServ)
		{
			if (Main.rand.NextBool(5))
				SpawnDust(Vector2.UnitY * -Main.rand.NextFloat(2), 150).noGravity = Main.rand.NextBool();

			if (SpawnTime > Params.SpawnTime / 3 && Main.rand.NextBool(15))
				SpawnSmoke(NPC.Bottom, -Vector2.UnitY, SpawnTime / (float)Params.SpawnTime * 0.17f, 90, EaseFunction.EaseCubicIn, GetSurfaceTile());
		}

		if (SpawnTime == Params.SpawnTime - 1)
		{
			if (!Main.dedServ)
			{
				Vector2 unit = new(Math.Clamp(Main.windSpeedCurrent, -1, 1), -1);

				for (int i = 0; i < 2; i++)
					SpawnSmoke(NPC.Bottom - Vector2.UnitY * 40 * i, unit, 0.3f, Main.rand.Next(100, 180), EaseFunction.EaseCircularOut, GetSurfaceTile());

				for (int i = 0; i < 4; i++)
				{
					float mag = Main.rand.NextFloat(5f);
					SpawnSmoke(NPC.Bottom + new Vector2(mag * unit.X * 10, 15 * -i), (unit * mag).RotatedByRandom(0.3f), Main.rand.NextFloat(0.1f, 0.17f), 60, EaseFunction.EaseCubicIn, GetSurfaceTile());
				}

				SpawnSmoke(NPC.Bottom, (Vector2.UnitY * -2f).RotatedByRandom(0.4f), 0.2f, 90, EaseFunction.EaseCubicIn, GetSurfaceTile());

				for (int i = 0; i < 15; i++)
					SpawnDust((Vector2.UnitY * -Main.rand.NextFloat(3, 5)).RotatedByRandom(1), 150).noGravity = Main.rand.NextBool();

				SoundEngine.PlaySound(SoundID.Run with { Pitch = 0.5f }, NPC.Center);
				SoundEngine.PlaySound(SoundID.NPCHit3 with { Pitch = 1f }, NPC.Center);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				SpawnStack(Main.rand.Next(3, 6) + (Main.expertMode ? 1 : 0));

				NPC.velocity.Y = -3;
				NPC.netUpdate = true;
			}
		}

		Dust SpawnDust(Vector2 velocity, int alpha) //Spawns dust based on surface tile type
		{
			var t = GetSurfaceTile();

			if (!t.HasTile)
				return new();

			var coords = NPC.Bottom.ToTileCoordinates();
			int i = coords.X;
			int j = coords.Y;

			var origin = NPC.BottomLeft;
			var dust = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, t)];
			float lightness = TileMaterial.FindMaterial(t.TileType).Lightness;

			dust.position = Main.rand.NextVector2FromRectangle(new Rectangle((int)origin.X, (int)origin.Y, NPC.width, 2));
			dust.velocity = velocity * (lightness + 0.25f);
			dust.alpha = alpha;

			return dust;
		}
	}

	public virtual void FallBehaviour()
	{
		NPC.rotation += NPC.velocity.X * 0.05f;

		if (Main.netMode != NetmodeID.MultiplayerClient && NPC.velocity.X == 0)
			NPC.velocity.X = Main.rand.NextFloat(-2f, 2f);

		if (NPC.collideX || NPC.collideY)
		{
			NPC.velocity.Y *= -0.9f;

			// Play a 'bounce' sound once for the initial fall.
			if (!_collisionSoundPlayed)
			{
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.75f }, NPC.Center);
				_collisionSoundPlayed = true;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				//Take continuous damage in contact with the ground but don't display it
				var hit = NPC.CalculateHitInfo(20, 1, damageVariation: true) with { HideCombatText = true };
				NPC.StrikeNPC(hit);

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendStrikeNPC(NPC, hit);

				NPC.velocity.X *= 1.1f;
				NPC.netUpdate = true;
			}
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (spawnInfo.PlayerInTown || !spawnInfo.Player.ZoneDesert || spawnInfo.SpawnTileType != TileID.Sand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;
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
		NPC.frame.Y = frameHeight * Segment switch
		{
			SegmentType.Base => 3,
			SegmentType.Middle => NPC.whoAmI % 2 + 1,
			SegmentType.Top => 0,
			_ => 1
		};

		int frameWidth = (NPC.frame.Width = 50) + 2;
		NPC.frame.Height = frameHeight - 2;

		NPC.frame.X = frameWidth * _style;
		_sine++;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Params.DustType, Scale: Main.rand.NextFloat(1f, 1.2f)).noGravity = Main.rand.NextBool();

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
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>(Name + "Top" + Main.rand.Next(1, 3)).Type);
				
				if (_style != 0)
				{
					string flowerName = Name + "Flower" + _style switch
					{
						2 => "Pink",
						3 => "Blue",
						4 => "White",
						5 => "Plum",
						_ => "Red"
					};

					for (int i = 1; i < 4; i++)
						Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>(flowerName + i).Type);
				}
			}
			else
			{
				for (int i = 0; i < 2; i++)
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>(Name + Main.rand.Next(1, 6)).Type);
			}

			for (int i = 6; i < 9; i++)
			{
				if (Main.rand.NextBool())
					Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>(Name + i).Type);
			}

			SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.5f }, NPC.Center);
			SoundEngine.PlaySound(Death, NPC.Center);
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

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => Falling ? false : null;

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon(ModContent.ItemType<Thornball>(), 1, 5, 10);

	#region hide
	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
	{
		if (SpawnTime < Params.SpawnTime)
			boundingBox = Rectangle.Empty; //Hide the bounding box when spawning in
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => SpawnTime >= Params.SpawnTime;
	public override bool CanHitNPC(NPC target) => SpawnTime >= Params.SpawnTime;
	#endregion

	#region helpers
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

	private Tile GetSurfaceTile()
	{
		var t = Framing.GetTileSafely(NPC.Bottom + Vector2.UnitY * 8);
		return WorldGen.SolidOrSlopedTile(t) ? t : new();
	}

	private static void SpawnSmoke(Vector2 position, Vector2 velocity, float scale, int duration, Color color, Color tertiaryColor, EaseFunction ease) 
		=> ParticleHandler.SpawnParticle(new SmokeCloud(position, velocity, color, scale, ease, duration)
	{
		Pixellate = true,
		PixelDivisor = 4,
		TertiaryColor = tertiaryColor
	});

	private static void SpawnSmoke(Vector2 position, Vector2 velocity, float scale, int duration, EaseFunction ease, Tile tile)
	{
		if (!tile.HasTile)
			return;

		var material = TileMaterial.FindMaterial(tile.TileType);
		var hsl = Main.rgbToHsl(material.Color);

		SpawnSmoke(position, velocity * (material.Lightness + 0.25f), scale, duration, material.Color, Main.hslToRgb(hsl with { X = hsl.X - 0.1f, Z = 0.5f }), ease);
	}
	#endregion

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		var source = NPC.frame;
		var origin = new Vector2(source.Width / 2, source.Height / 2 + 6);
		var color = NPC.GetAlpha(drawColor);

		if (NPC.IsABestiaryIconDummy) //Draw the Bestiary entry
		{
			int frameHeight = texture.Height / Main.npcFrameCount[Type];
			center.Y -= 6;

			Main.EntitySpriteDraw(texture, center, source, color, NPC.rotation, origin, NPC.scale, default);
			Main.EntitySpriteDraw(texture, center - new Vector2(GetSway(1), NPC.height), source with { Y = source.Y - frameHeight }, color, NPC.rotation, origin, NPC.scale, default);
			Main.EntitySpriteDraw(texture, center - new Vector2(GetSway(2), NPC.height * 2), source with { Y = source.Y - frameHeight * 3 }, color, NPC.rotation, origin, NPC.scale, default);

			DrawFace(center - new Vector2(GetSway(2), NPC.height * 2), color);

			return false;
		}

		Main.EntitySpriteDraw(texture, center, source, color, NPC.rotation, origin, NPC.scale, default);

		if (Segment is SegmentType.Top)
			DrawFace(center, color);

		return false;

		float GetSway(int index) => (float)Math.Sin((_sine + index * 25f) / 10f) * 2;
	}

	private void DrawFace(Vector2 center, Color color)
	{
		Rectangle source = FaceTexture.Frame(1, 2, 0, (int)(_sine % 300) / 285, sizeOffsetY: -2);

		Vector2 target = NPC.IsABestiaryIconDummy ? Main.MouseScreen : Main.player[NPC.target].Center;
		float turn = (int)(Math.Clamp((target.X - NPC.Center.X) / 50f, -4, 4) / 2) * 2;

		Main.EntitySpriteDraw(FaceTexture, center + Vector2.UnitX * turn, source, color, NPC.rotation, source.Size() / 2, NPC.scale, default);
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(Falling);
		writer.Write(_style);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		Falling = reader.ReadBoolean();
		_style = reader.ReadByte();
	}

	public bool TallyDeath(NPC npc) => (npc.ModNPC as Stactus).Segment is SegmentType.Top; //Only ever tally the head
}