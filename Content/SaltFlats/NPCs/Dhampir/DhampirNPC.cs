using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.SaltFlats.Biome;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs.Dhampir;

internal class DhampirNPC : ModNPC
{
	public struct MistParticle(NPC parent)
	{
		public static Asset<Texture2D> Texture = ModContent.Request<Texture2D>("SpiritReforged/Content/SaltFlats/NPCs/Dhampir/DhampirNPCParticles");

		public Vector2 Position = parent.Center;
		public Vector2 Velocity = parent.velocity + Main.rand.NextVector2Circular(-3, 3);
		public NPC Parent = parent;
		public int Timer = 0;
		public int Variant = Main.rand.Next(3);
		public Color Color = DetermineParticleColor();
		public float Rotation = 0f;

		private static Color DetermineParticleColor()
		{
			byte darkness = (byte)Main.rand.Next(170, 255);
			return new Color((byte)(darkness * Main.rand.NextFloat(0.5f, 1f)), darkness, (byte)Main.rand.Next(darkness, 256), 0) * Main.rand.NextFloat(0.95f, 1f);
		}

		public void Update()
		{
			Position += Velocity;
			Position += Parent.velocity * 0.1f;
			Velocity *= 0.92f;
			Rotation += Velocity.X * 0.01f;
		}

		public readonly void Draw(Vector2 screenPos)
		{
			var src = new Rectangle(32 * Variant, (int)(Timer / 19f) * 32, 30, 30);
			Main.spriteBatch.Draw(Texture.Value, Position - screenPos, src, Lighting.GetColor(Position.ToTileCoordinates(), Color), Rotation, Vector2.Zero, 1, 0, 0);
		}
	}

	public enum DhampirState
	{
		Waiting,
		Run,
		Fly,
	}

	private Player Target => Main.player[NPC.target];

	public ref DhampirState State => ref Unsafe.As<float, DhampirState>(ref NPC.ai[0]);
	private ref float Timer => ref NPC.ai[1];
	private ref float FlyTargetDirection => ref NPC.ai[2];

	private bool LastCollideY
	{
		get => NPC.ai[3] == 1;
		set => NPC.ai[3] = value ? 1 : 0;
	}

	private bool ExpertQuickSlam
	{
		get => NPC.localAI[0] == 1;
		set => NPC.localAI[0] = value ? 1 : 0;
	}

	private readonly List<MistParticle> _particles = [];

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 8;

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(28, 52);
		NPC.lifeMax = 2000;
		NPC.damage = 50;
		NPC.defense = 20;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0;
		NPC.dontTakeDamage = true;
		NPC.noGravity = true;

		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.damage = ModeUtils.ByMode(20, 40, 70, 100);
		NPC.defense = ModeUtils.ByMode(10, 25, 33, 50);
		NPC.lifeMax = ModeUtils.ByMode(1000, 2000, 2500, 5000);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		NPC.velocity.Y += 0.035f + MathF.Max(0, NPC.velocity.Y * 0.025f);

		Point tilePos = NPC.Bottom.ToTileCoordinates();
		int dist = 0;

		foreach (ref MistParticle particle in CollectionsMarshal.AsSpan(_particles))
		{
			particle.Update();
			particle.Timer++;
		}

		_particles.RemoveAll(x => x.Timer > 50);

		while (!WorldGen.SolidTile(tilePos.X, tilePos.Y, true))
		{
			dist++;
			tilePos.Y++;

			if (dist > 4)
				break;
		}

		if (State == DhampirState.Waiting)
		{
			foreach (Player player in Main.ActivePlayers)
			{
				if (player.DistanceSQ(NPC.Center) < 400 * 400)
				{
					State = DhampirState.Run;
					NPC.dontTakeDamage = false;
					Timer = 10;
					break;
				}
			}
		}
		else if (State == DhampirState.Run)
		{
			NPC.TargetClosest();

			if (NPC.velocity.Y < 0)
				NPC.velocity.Y *= 0.95f;

			if (Math.Abs(NPC.Center.X - Target.Center.X) < 50 && NPC.Top.Y > Target.Bottom.Y)
				NPC.velocity.Y = -6;

			int direction = MathF.Sign(Target.Center.X - NPC.Center.X);
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * (Main.expertMode ? 9 : 7), !NPC.collideY ? 0.033f : (Main.expertMode ? 0.15f : 0.1f));
			NPC.direction = NPC.spriteDirection = -direction;
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width,	 NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

			if (Collision.SolidCollision(NPC.TopLeft - new Vector2(6, 0), 6, NPC.height - 4))
				SetFly();
			else if (Collision.SolidCollision(NPC.TopRight, 6, NPC.height - 4))
				SetFly();

			Timer += MathF.Max(0, 1 - NPC.Distance(Target.Center) / 400f);

			if (Timer >= 100 - NPC.life / NPC.lifeMax * 30)
				SetFly();

			if (!LastCollideY && NPC.collideY)
			{
				tilePos = NPC.Bottom.ToTileCoordinates();
				Tile tile = Main.tile[tilePos];
				int count = ExpertQuickSlam ? 12 : 3;

				for (int i = 0; i < count; ++i)
				{
					int dustIndex = WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, tile);
					Dust dust = Main.dust[dustIndex];
					dust.position = NPC.BottomLeft + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(-2, 2));
					dust.velocity = new Vector2(Main.rand.NextFloat(-2, 2), -1);

					if (i < 6 && ExpertQuickSlam)
					{
						var particle = new MistParticle(NPC) { Timer = Main.rand.Next(10) + 10 };
						particle.Velocity = Main.rand.NextVector2Circular(6, 6);

						if (ExpertQuickSlam)
							particle.Velocity.Y -= 6;

						_particles.Add(particle);
					}
				}
			}
		}
		else if (State == DhampirState.Fly)
		{
			for (int i = 0; i < Main.rand.Next(3); ++i)
				if (Main.rand.NextFloat() < NPC.life / (float)NPC.lifeMax)
					SpawnMist();

			Vector2 targetPos = Target.Center + new Vector2(FlyTargetDirection, -250);
			Timer++;
			NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPos) * 12, 0.05f + Timer * 0.001f);

			if (NPC.DistanceSQ(targetPos) < 10 * 10)
			{
				Timer = 0;
				State = DhampirState.Run;
				NPC.dontTakeDamage = false;
				NPC.noTileCollide = false;
				NPC.damage = NPC.defDamage;
				ExpertQuickSlam = false;

				if (Main.expertMode)
				{
					ExpertQuickSlam = Main.rand.NextBool();
					NPC.velocity.Y = 8;
				}

				for (int i = 0; i < 16; ++i)
				{
					var particle = new MistParticle(NPC) { Timer = Main.rand.Next(15) };
					particle.Velocity = Main.rand.NextVector2Circular(12, 12);

					if (ExpertQuickSlam)
						particle.Velocity.Y -= 6;

					_particles.Add(particle);
				}
			}
		}

		LastCollideY = NPC.collideY;
	}

	public void SetFly()
	{
		State = DhampirState.Fly;
		FlyTargetDirection = MathF.Sign(Target.Center.X - NPC.Center.X) * Main.rand.NextFloat(200, 550);

		if (Math.Abs(Target.velocity.X) < 3)
			FlyTargetDirection = MathF.Sign(Target.Center.X - NPC.Center.X) * Main.rand.NextFloat(50, 150);

		Timer = 0;
		NPC.dontTakeDamage = true;
		NPC.noTileCollide = true;

		if (!Main.getGoodWorld)
			NPC.damage = 0;

		for (int i = 0; i < 12; ++i)
		{
			Vector2 pos = Main.rand.NextVector2FromRectangle(NPC.Hitbox);
			Vector2 vel = Main.rand.NextVector2Circular(2, 2) - new Vector2(0, 2);
			Dust.NewDustPerfect(pos, ModContent.DustType<MistDust>(), vel, Main.rand.Next(120, 180), Scale: Main.rand.NextFloat(1, 1.5f)).customData = NPC;
			SpawnMist();
		}
	}

	private void SpawnMist() => _particles.Add(new MistParticle(NPC) { Timer = Main.rand.Next(15) });

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			for (int i = 0; i < 3; ++i)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke);

			if (NPC.life <= 0)
			{
				for (int i = 0; i < 4; ++i)
				{
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, newColor: Color.Lerp(Color.Gray, new Color(20, 20, 20, 255), Main.rand.NextFloat()));
					Gore.NewGore(NPC.GetSource_Death(), NPC.Center, Main.rand.NextVector2Circular(2, 2), 99);
				}
			}
		}
	}

	public override void FindFrame(int frameHeight)
	{
		const int FrameWidth = 156 / 3;

		NPC.frame.Width = 50;
		NPC.frame.Height = 58;
		NPC.frameCounter++;

		if (NPC.IsABestiaryIconDummy)
		{
			NPC.frame.X = FrameWidth * 2;
			NPC.frame.Y = frameHeight * (int)(NPC.frameCounter / 10f % 4);
			return;
		}	

		if (State == DhampirState.Run)
		{
			if (!NPC.collideY)
			{
				NPC.frame.X = FrameWidth * 2;
				NPC.frame.Y = frameHeight * (int)(NPC.frameCounter / 10f % 4);
				return;
			}

			NPC.frame.X = FrameWidth;
			NPC.frame.Y = frameHeight * (int)(NPC.frameCounter / 4f % 8);
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		foreach (ref MistParticle particle in CollectionsMarshal.AsSpan(_particles))
			particle.Draw(screenPos);

		if (State == DhampirState.Fly)
		{
			if (Reflections.DrawingReflection)
				return false;

			return false;
		}

		if (!Reflections.DrawingReflection && !NPC.IsABestiaryIconDummy)
			return false;

		SpriteEffects flip = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Texture2D tex = TextureAssets.Npc[Type].Value;

		if (NPC.IsABestiaryIconDummy)
			screenPos += new Vector2(-4, 2);

		spriteBatch.Draw(tex, NPC.Center - screenPos, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, 1f, flip, 0);
		return false;
	}

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
}
