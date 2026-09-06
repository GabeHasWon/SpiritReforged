using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.SaltFlats.Biome;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria.Cinematics;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.SaltFlats.NPCs.Dhampir;

internal class DhampirNPC : ModNPC
{
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

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 8;

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(28, 52);
		NPC.lifeMax = 2000;
		NPC.defense = 20;
		NPC.aiStyle = -1;
		NPC.knockBackResist = 0;
		NPC.dontTakeDamage = true;
		NPC.noGravity = true;

		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		NPC.velocity.Y += 0.035f + MathF.Max(0, NPC.velocity.Y * 0.025f);

		Point tilePos = NPC.Bottom.ToTileCoordinates();
		int dist = 0;
		
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
					break;
				}
			}
		}
		else if (State == DhampirState.Run)
		{
			NPC.TargetClosest();

			if (NPC.velocity.Y < 0)
				NPC.velocity.Y *= 0.95f;

			int direction = MathF.Sign(Target.Center.X - NPC.Center.X);
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * 7, !NPC.collideY ? 0.033f : 0.1f);
			NPC.direction = NPC.spriteDirection = -direction;
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width,	 NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

			if (Collision.SolidCollision(NPC.TopLeft - new Vector2(6, 0), 6, NPC.height - 4))
				SetFly();
			else if (Collision.SolidCollision(NPC.TopRight, 6, NPC.height - 4))
				SetFly();

			Timer += MathF.Max(0, 1 - NPC.Distance(Target.Center) / 400f);

			if (Timer >= 100 - (NPC.life / NPC.lifeMax) * 30)
				SetFly();

			if (!LastCollideY && NPC.collideY)
			{
				tilePos = NPC.Bottom.ToTileCoordinates();
				Tile tile = Main.tile[tilePos];

				for (int i = 0; i < 3; ++i)
				{
					int dustIndex = WorldGen.KillTile_MakeTileDust(tilePos.X, tilePos.Y, tile);
					Dust dust = Main.dust[dustIndex];
					dust.position = NPC.BottomLeft + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(-2, 2));
					dust.velocity = new Vector2(Main.rand.NextFloat(-2, 2), -1);
				}
			}
		}
		else if (State == DhampirState.Fly)
		{
			Vector2 targetPos = Target.Center + new Vector2(FlyTargetDirection, -250);
			Timer++;
			NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionTo(targetPos) * 12, 0.05f + Timer * 0.001f);

			if (NPC.DistanceSQ(targetPos) < 10 * 10)
			{
				Timer = 0;
				State = DhampirState.Run;
				NPC.dontTakeDamage = false;
				NPC.noTileCollide = false;
			}
		}

		LastCollideY = NPC.collideY;
	}

	public void SetFly()
	{
		State = DhampirState.Fly;
		FlyTargetDirection = MathF.Sign(Target.Center.X - NPC.Center.X) * 550;
		Timer = 0;
		NPC.dontTakeDamage = true;
		NPC.noTileCollide = true;

		for (int i = 0; i < 12; ++i)
		{
			Vector2 pos = Main.rand.NextVector2FromRectangle(NPC.Hitbox);
			Vector2 vel = Main.rand.NextVector2Circular(2, 2) - new Vector2(0, 2);
			Dust.NewDustPerfect(pos, ModContent.DustType<MistDust>(), vel, Main.rand.Next(120, 180), Scale: Main.rand.NextFloat(1, 1.5f)).customData = NPC;
		}
	}

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
}
