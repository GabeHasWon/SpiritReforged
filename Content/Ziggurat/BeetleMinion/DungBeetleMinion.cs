using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using System.IO;

namespace SpiritReforged.Content.Ziggurat.BeetleMinion;

[AutoloadMinionBuff]
public class DungBeetleMinion : BaseMinion
{
	public const int FlourishCounterMax = 20;

	public int ChildWhoAmI { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }

	public ref float MovementCounter => ref Projectile.ai[1];

	public ref float FlourishCounter => ref Projectile.ai[2];

	public bool Flourishing => FlourishCounter > 0;

	public Projectile Child
	{
		get
		{
			Projectile value = Main.projectile[ChildWhoAmI];
			return (value.active && value.ModProjectile is DungBall) ? value : null;
		}
	}

	public DungBeetleMinion() : base(500, 500, new Vector2(24)) { }

	public override void SetDefaults()
	{
		base.SetDefaults();
		Projectile.tileCollide = true;
	}

	public override void AI()
	{
		const int ball_frequency = 250;
		Player owner = Main.player[Projectile.owner];

		if (Child == null)
		{
			ScarabMovement(Projectile);
			Projectile.direction = Math.Sign(Projectile.velocity.X);

			if (Flourishing)
			{
				FlourishCounter--;
				Projectile.rotation = FlourishCounter / FlourishCounterMax * MathHelper.Pi * Projectile.direction;
			}
		}
		else
		{
			Projectile.Bottom = Child.Top;
			Projectile.velocity = Child.velocity;
			Projectile.gfxOffY = Child.gfxOffY;

			FlourishCounter = 0; //Reset flourish animations in case they were playing
			Projectile.rotation = 0;
		}

		if ((MovementCounter += Math.Abs(Projectile.velocity.X)) >= ball_frequency)
		{
			MovementCounter -= ball_frequency;
			bool spawnDusts = !Main.dedServ;

			if (Child == null)
			{
				if (Main.myPlayer == Projectile.owner)
				{
					ChildWhoAmI = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<DungBall>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.whoAmI);
					Projectile.netUpdate = true;
				}
			}
			else
			{
				if ((Child.ModProjectile as DungBall).Size++ >= DungBall.MaxSize)
				{
					spawnDusts = false;
				}
				else
				{
					Child.velocity.Y -= 4;
					Child.velocity.X *= 1.2f;
				}
			}

			if (spawnDusts)
			{
				for (int i = 0; i < 20; i++)
				{
					var dust = Dust.NewDustDirect(Child.position, Child.width, Child.height, DustID.Dirt, 0, -1);
					dust.fadeIn = 1.1f;

					if (Main.rand.NextBool())
					{
						dust.noGravity = true;
						dust.alpha = 180;
						dust.scale *= 2;
						dust.fadeIn *= 2;
					}
				}
			}
		}

		ScarabStaff.DungBeetlePlayer dungBeetlePlayer = owner.GetModPlayer<ScarabStaff.DungBeetlePlayer>();

		if (!owner.HasMinionAttackTargetNPC)
			dungBeetlePlayer.struckTarget = false;

		if (dungBeetlePlayer.struckTarget && Child?.ModProjectile is DungBall dungBall)
		{
			NPC target = Main.npc[owner.MinionAttackTargetNPC];
			dungBall.Launch(target, 10); //Launch the projectile, if any
			dungBeetlePlayer.struckTarget = false;

			ChildWhoAmI = 0;
			FlourishCounter = FlourishCounterMax;
			Projectile.velocity.Y -= 5;
			Projectile.velocity.X -= Projectile.direction * 3;

			Projectile.netUpdate = true;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity) => false;

	public override bool MinionContactDamage() => false;

	public static void ScarabMovement(Projectile projectile)
	{
		const float max_gravity = 4;
		const float max_speed = 6;
		const float acceleration = 0.01f;

		Player owner = Main.player[projectile.owner];
		float gravity = Math.Min(projectile.velocity.Y + 0.5f, max_gravity);
		int direction = projectile.Center.X < owner.Center.X ? 1 : -1;

		projectile.velocity = new Vector2(MathHelper.Lerp(projectile.velocity.X, max_speed * direction, acceleration), gravity);
		Collision.StepUp(ref projectile.position, ref projectile.velocity, projectile.width, projectile.height, ref projectile.stepSpeed, ref projectile.gfxOffY);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.spriteDirection = Projectile.direction;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(3, 2, Child is null ? 0 : 1, 0, -2, -2);
		SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);
		return false;
	}
}

public class DungBall : ModProjectile
{
	public const int MaxSize = 2; //Indexed by zero

	public int ParentWhoAmI { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }

	public int Size { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = Math.Min(value, MaxSize); }

	public int Counter { get => (int)Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

	private static readonly Vector2[] _dimensions = [new(14), new(30), new(42)];
	private bool _launched;

	public Projectile Parent
	{
		get
		{
			Projectile value = Main.projectile[ParentWhoAmI];
			return (value.active && value.ModProjectile is DungBeetleMinion) ? value : null;
		}
	}

	public override string Texture => ModContent.GetInstance<DungBeetleMinion>().Texture;

	public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

	public override void SetDefaults()
	{
		Projectile.Size = new(16);
		Projectile.minion = true;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Summon;
	}

	public override void AI()
	{
		if (_launched)
		{
			LaunchedAI();
		}
		else
		{
			Projectile.Size = _dimensions[Size];

			if (Parent == null)
				_launched = true;
			else
				DungBeetleMinion.ScarabMovement(Projectile);

			if (Main.rand.NextFloat() < Math.Abs(Projectile.velocity.X / 5f) && Collision.SolidCollision(Projectile.position + new Vector2(0, 2), Projectile.width, Projectile.height))
			{
				Dust dust = Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 2, DustID.Dirt, Alpha: 80, Scale: 1.2f);
				dust.velocity = Projectile.velocity * -0.5f - Vector2.UnitY;
				dust.noGravity = !Main.rand.NextBool(4);
			}
		}

		Projectile.rotation += Projectile.velocity.X * 0.05f;
		Counter++;
	}

	public void LaunchedAI()
	{
		const int phase_time = 20;

		Projectile.tileCollide = Counter > phase_time;
		Projectile.velocity.Y += 0.1f;
	}

	public override bool OnTileCollide(Vector2 oldVelocity) => _launched;

	public override bool? CanDamage() => _launched;

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= 1f + (float)Size / MaxSize; //Multiply damage based on Size

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Poisoned, 60 * 5 * (Size + 1));

	public override void OnKill(int timeLeft)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 15; i++)
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextFromList(DustID.Dirt, DustID.ToxicBubble), 0, -1);
			dust.fadeIn = 1.1f;

			if (Main.rand.NextBool())
			{
				dust.noGravity = true;
				dust.alpha = 180;
				dust.scale *= 2;
				dust.fadeIn *= 2;
			}
		}

		for (int i = 0; i < 3 * Size; i++)
		{
			int type = Main.rand.Next(1, Size > 0 ? 7 : 4);
			Gore.NewGore(Projectile.GetSource_Death(), Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Vector2.Normalize(-Projectile.velocity), Mod.Find<ModGore>("Dung" + type).Type, 1f);
		}
	}

	public void Launch(NPC target, float speed)
	{
		_launched = true;

		if (Collision.CanHit(Projectile, target))
			Projectile.velocity = ArcVelocityHelper.GetArcVel(Projectile.Center, target.Center, 0.1f, speed);
		else
			Projectile.velocity = ArcVelocityHelper.GetArcVel(Projectile.Center, target.Center, 0.1f, speed, useHigherAngle: true);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(3, 2, Size, 1, -2, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_launched);
	public override void ReceiveExtraAI(BinaryReader reader) => _launched = reader.ReadBoolean();
}