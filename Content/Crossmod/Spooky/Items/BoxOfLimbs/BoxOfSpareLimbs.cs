using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;
using System.IO;

namespace SpiritReforged.Content.Crossmod.Spooky.Items.BoxOfLimbs;

internal class BoxOfSpareLimbs : ModItem, IFlagged
{
	public class SpareHeart : ModProjectile
	{
		private ref float WalkTime => ref Projectile.ai[0];
		private ref float IdleTime => ref Projectile.ai[1];
		private ref float Timer => ref Projectile.ai[2];

		private ref float WalkDir => ref Projectile.localAI[0];

		public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

		public override void SetDefaults()
		{
			Projectile.timeLeft = 1800;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.Size = new Vector2(26);
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.localNPCHitCooldown = 30;
			Projectile.usesLocalNPCImmunity = true;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) => false;
		public override bool? CanCutTiles() => false;

		public override void AI()
		{
			if (IdleTime < 30)
			{
				Projectile.velocity.Y += 0.3f;
				IdleTime++;
				return;
			}

			Projectile.velocity.Y += 0.3f;

			if (Math.Abs(Projectile.velocity.X) > 0.1f)
				Projectile.direction = Projectile.spriteDirection = MathF.Sign(Projectile.velocity.X);

			if (WalkTime == 0)
				WalkTime = Main.rand.NextFloat(120, 180);

			Timer++;

			if (Timer > WalkTime)
			{
				if (WalkDir == 0 && Main.myPlayer == Projectile.owner)
				{
					WalkDir = Main.rand.NextBool() ? -1 : 1;
					Projectile.netUpdate = true;
				}

				if (Timer > WalkTime + 60)
				{
					Timer = 0;
					WalkTime = Main.rand.NextFloat(120, 180);
					Projectile.netUpdate = true;
				}

				if (Projectile.velocity.X == 0 && WalkDir == 1 && Collision.SolidCollision(Projectile.BottomRight - new Vector2(0, 20), 6, 18))
					WalkDir *= -1;

				if (Projectile.velocity.X == 0 && WalkDir == -1 && Collision.SolidCollision(Projectile.BottomLeft - new Vector2(6, 20), 6, 18))
					WalkDir *= -1;

				Projectile.velocity.X = WalkDir;
				Projectile.frameCounter += 1;
				Projectile.frame = (int)(Projectile.frameCounter / 4f) % 4;
			}
			else
			{
				Projectile.velocity.X *= 0.9f;
				Projectile.frame = 4;
			}
		}
	}

	public class SpareHand : ModProjectile
	{
		private int AttachedNpc
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private int Target
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		private bool Initialized
		{
			get => Projectile.ai[2] == 1;
			set => Projectile.ai[2] = value ? 1 : 0;
		}

		public Vector2 AttachOffset
		{
			get => new(Projectile.localAI[0], Projectile.localAI[1]);
			set => (Projectile.localAI[0], Projectile.localAI[1]) = (value.X, value.Y);
		}

		private ref float AnimTimer => ref Projectile.localAI[2];

		private float IdleTime = 0;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.timeLeft = 600;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.Size = new Vector2(26);
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.localNPCHitCooldown = 30;
			Projectile.usesLocalNPCImmunity = true;
		}

		public override bool? CanCutTiles() => false;
		public override bool? CanDamage() => IdleTime >= 30;

		public override void AI()
		{
			if (!Initialized)
			{
				Initialized = true;
				AttachedNpc = -1;
			}

			if (IdleTime < 30)
			{
				IdleTime++;
				Projectile.velocity.Y += 0.3f;
				return;
			}

			if (AttachedNpc != -1)
			{
				NPC attach = Main.npc[AttachedNpc];

				if (!attach.CanBeChasedBy())
				{
					AttachedNpc = -1;
					Projectile.netUpdate = true;
					Projectile.tileCollide = true;
					return;
				}

				Projectile.velocity = Vector2.Zero;
				Projectile.Center = attach.Center - AttachOffset;
				Projectile.frame = 2;
				Projectile.rotation = AttachOffset.ToRotation() - MathHelper.PiOver2;

				return;
			}

			Target = -1;
			AnimTimer += Projectile.velocity.X * 0.2f;

			Projectile.frame = (int)(AnimTimer / 3f % 2) == 0 ? 0 : 1;

			if (Projectile.velocity.X < 0.1f)
				Projectile.frame = 0;

			Projectile.rotation = Projectile.velocity.X * 0.04f;

			if (Math.Abs(Projectile.velocity.X) > 0.1f)
				Projectile.direction = Projectile.spriteDirection = -MathF.Sign(Projectile.velocity.X);

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.CanBeChasedBy())
					continue;

				if (Target == -1)
				{
					if (npc.DistanceSQ(Projectile.Center) < 800 * 800)
					{
						Target = npc.whoAmI;
						break;
					}
					else
						continue;
				}

				if (Projectile.DistanceSQ(Main.npc[Target].Center) > npc.DistanceSQ(Projectile.Center))
				{
					Target = npc.whoAmI;
					break;
				}
			}

			Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

			if (Target != -1)
			{
				NPC target = Main.npc[Target];
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, MathF.Sign(target.Center.X - Projectile.Center.X) * 6, 0.05f);

				if (Projectile.velocity.Y == 0 && Math.Abs(Projectile.Center.X - target.Center.X) < target.width / 2f)
				{
					float dist = MathHelper.Clamp((Projectile.Center.Y - Main.npc[Target].Center.Y) / 18f, 0, 14);
					Projectile.velocity.Y = -dist - 1;
				}

				if (target.Center.Y > Projectile.Center.Y && Projectile.velocity.Y < 0)
					Projectile.velocity.Y *= 0.95f;
			}
			else
			{
				Projectile.velocity.X *= 0.95f;
				Projectile.frameCounter++;

				if (Projectile.frameCounter == 60)
				{
					Projectile.frameCounter = 0;
					Projectile.velocity.X = Main.rand.NextFloat(2, 3) * (Main.rand.NextBool() ? -1 : 1);
					Projectile.netUpdate = true;
				}
			}

			if (Projectile.velocity.Y == 0)
			{
				if (Collision.SolidCollision(Projectile.BottomRight - new Vector2(0, 20), 6, 18))
					Projectile.velocity.Y = -8;

				if (Collision.SolidCollision(Projectile.BottomLeft - new Vector2(6, 20), 6, 18))
					Projectile.velocity.Y = -8;
			}

			Projectile.velocity.Y += 0.3f;

			if (Projectile.timeLeft < 15)
				Projectile.velocity *= 0.9f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) => false;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 12; ++i)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Sand);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (AttachedNpc != -1)
			{
				Projectile.damage = (int)(Projectile.damage * 0.8f);

				if (Projectile.damage < 5)
					Projectile.Kill();
			}
			else
			{
				AttachedNpc = target.whoAmI;
				var factor = new Vector2(Main.rand.NextFloat(target.width / -2, target.width / 2), Main.rand.NextFloat(target.height / -2, target.height / 2));
				AttachOffset = Vector2.Normalize(target.Center - Projectile.Center) * factor;
				Projectile.tileCollide = false;
			}

			Projectile.netUpdate = true;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write((Half)Projectile.localAI[0]);
			writer.Write((Half)Projectile.localAI[1]);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			Projectile.localAI[0] = (float)reader.ReadHalf();
			Projectile.localAI[1] = (float)reader.ReadHalf();
		}
	}

	public class SpareLeg : ModProjectile
	{
		private ref float Timer => ref Projectile.ai[0];

		private int Target
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		private ref float IdleTime => ref Projectile.ai[2];

		public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

		public override void SetDefaults()
		{
			Projectile.timeLeft = 600;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.Size = new Vector2(22, 34);
			Projectile.friendly = true;
			Projectile.penetrate = 3;
		}

		public override bool? CanCutTiles() => false;
		public override bool? CanDamage() => IdleTime >= 30;

		public override void AI()
		{
			if (IdleTime < 30)
			{
				Projectile.velocity.Y += 0.3f;
				IdleTime++;
				return;
			}

			Target = -1;

			if (Math.Abs(Projectile.velocity.X) > 0.1f)
				Projectile.direction = Projectile.spriteDirection = MathF.Sign(Projectile.velocity.X);

			Projectile.velocity.Y += 0.3f;
			Projectile.rotation = Projectile.velocity.X * 0.04f;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.CanBeChasedBy())
					continue;

				if (Target == -1)
				{
					if (npc.DistanceSQ(Projectile.Center) < 800 * 800)
					{
						Target = npc.whoAmI;
						break;
					}
					else
						continue;
				}

				if (Projectile.DistanceSQ(Main.npc[Target].Center) > npc.DistanceSQ(Projectile.Center))
				{
					Target = npc.whoAmI;
					break;
				}
			}

			if (Collision.SolidCollision(Projectile.BottomLeft, Projectile.width, 6, true))
			{
				Projectile.velocity.X *= 0.7f;
				Timer++;

				int max = Target == -1 ? 60 : 30;

				if (Timer < max * 0.67f)
					Projectile.frame = 0;
				else
					Projectile.frame = 1;

				if (Timer > max)
				{
					if (Target == -1)
					{
						Projectile.velocity.X = Main.rand.NextFloat(-4, 4);
						Projectile.netUpdate = true;
						Projectile.velocity.Y = -7;
					}
					else
					{
						float dist = MathHelper.Clamp((Projectile.Center.Y - Main.npc[Target].Center.Y) / 10f, 0, 14);
						Projectile.velocity.Y = -dist - 1;
						Projectile.velocity.X = MathF.Sign(Main.npc[Target].Center.X - Projectile.Center.X) * 4;
					}

					Timer = 0;
				}
			}
			else
			{
				if (Projectile.velocity.Y < -3)
					Projectile.frame = 2;
				else if (Projectile.velocity.Y < 1)
					Projectile.frame = 3;
				else
					Projectile.frame = 4;

				if (Target == -1)
					return;
				
				NPC target = Main.npc[Target];
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, MathF.Sign(target.Center.X - Projectile.Center.X) * 4, 0.05f);

				if (target.Center.Y > Projectile.Center.Y && Projectile.velocity.Y < 0)
					Projectile.velocity.Y *= 0.95f;
			}

			if (Projectile.timeLeft < 15)
				Projectile.velocity *= 0.9f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) => false;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 12; ++i)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Sand);
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write((Half)Projectile.velocity.X);
		public override void ReceiveExtraAI(BinaryReader reader) => Projectile.velocity.X = (float)reader.ReadHalf();
	}

	public class SpareLimbPlayer : ModPlayer
	{
		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (Player.CheckFlag(nameof(BoxOfSpareLimbs)) == true)
				modifiers.DisableDust();
		}

		public override void OnHurt(Player.HurtInfo info)
		{
			if (Player.CheckFlag(nameof(BoxOfSpareLimbs)) == true)
			{
				for (int i = 0; i < 6; ++i)
					Dust.NewDust(Player.position, Player.width, Player.head, DustID.Sand, Player.velocity.X, Player.velocity.Y);

				int damage = (int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(40);
				int type = Main.rand.NextBool() ? ModContent.ProjectileType<SpareLeg>() : ModContent.ProjectileType<SpareHand>();
				Vector2 velocity = Player.velocity * 0.2f + new Vector2(Main.rand.NextFloat(-2, 2), -6);
				Terraria.DataStructures.IEntitySource src = Player.GetSource_OnHurt(info.DamageSource);
				Projectile.NewProjectile(src, Player.Center, velocity, type, damage, 0, Player.whoAmI);

				if (Main.rand.NextBool(5))
					Projectile.NewProjectile(src, Player.Center, velocity, ModContent.ProjectileType<SpareHeart>(), 0, 0, Player.whoAmI);
			}
		}
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Aglet);
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;
		Item.value = Item.buyPrice(0, 1, 0, 0);
		Item.damage = 40;
		Item.DamageType = DamageClass.Summon;
	}
}
