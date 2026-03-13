using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;
using Terraria.GameContent.UI.States;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

internal class LocustCrook : ModItem
{
	private class LocustCrookProjectile : ModProjectile 
	{
		private Player Owner => Main.player[Projectile.owner];

		private bool Embedded
		{
			get => Projectile.ai[0] == 1;
			set => Projectile.ai[0] = value ? 1 : 0;
		}

		private ref float Timer => ref Projectile.ai[1];

		private bool Held
		{
			get => Projectile.ai[2] == 0;
			set => Projectile.ai[2] = value ? 0 : 1;
		}

		private ref float ThrowTimer => ref Projectile.localAI[0];
		private ref float Rotation => ref Projectile.localAI[1];

		private bool VisuallyHeld
		{
			get => Projectile.localAI[2] == 0;
			set => Projectile.localAI[2] = value ? 0 : 1;
		}

		public override string Texture => base.Texture.Replace("Projectile", "");

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.Size = new Vector2(14);
			Projectile.timeLeft = Projectile.SentryLifeTime;
			Projectile.sentry = false;
		}

		public override bool? CanDamage() => false;

		public void Throw()
		{
			Held = false;
			Rotation = Projectile.rotation + MathHelper.PiOver2 * 1.5f;
			ThrowTimer = 30;
		}

		public override void AI()
		{
			// Oh man this is really messy...
			if (!Held && Owner.itemAnimation > 0)
			{
				if (Owner.itemAnimation == Owner.itemAnimationMax / 2)
				{
					Projectile.sentry = true;
					Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld).RotatedBy(0.32f) * 12;
					Owner.UpdateMaxTurrets();

					VisuallyHeld = false;
				}
			}

			if (VisuallyHeld)
			{
				const float MaxTurn = 1.6f;

				Vector2 mouse = PlayerMouseHandler.GetMouse(Projectile.owner);
				Owner.ChangeDir(Math.Sign(mouse.X - Owner.Center.X));

				float factor = Owner.itemAnimation / (float)Owner.itemAnimationMax;
				float anim = MathHelper.Lerp(MathHelper.Lerp(0, MaxTurn, factor), MathHelper.Lerp(MaxTurn, 0, factor), factor);

				Projectile.rotation = Projectile.AngleTo(mouse) - MathHelper.PiOver2 * 1.5f + anim * Owner.direction;
				Projectile.Center = Owner.Center + Projectile.DirectionTo(mouse).RotatedBy(Owner.direction == 1 ? -MathHelper.PiOver2 : MathHelper.PiOver2) * 14;

				if (Owner.itemAnimation > 0)
				{
					Vector2 dir = Projectile.DirectionTo(Main.MouseWorld);
					Projectile.velocity += dir * (Owner.itemAnimationMax - Owner.itemAnimation) * 0.8f;
					Projectile.rotation = MathHelper.Lerp(Projectile.rotation, dir.ToRotation() - MathHelper.PiOver2 * 1.5f, (float)Owner.itemAnimation / Owner.itemAnimationMax);
				}

				if (Owner.direction == 1)
					Projectile.rotation -= MathHelper.Pi;

				Owner.heldProj = Projectile.whoAmI;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + MathHelper.PiOver2 * 1.5f);
			}
			// ...but it looks kinda okay!
			
			if (Held)
			{
				if (Owner.HeldItem.type != ModContent.ItemType<LocustCrook>())
				{
					Projectile.Kill();
					return;
				}

				return;
			}

			if (!Embedded)
			{
				if (!VisuallyHeld)
					Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2 * 1.5f;
				
				Projectile.velocity.Y += 0.16f;
				Projectile.velocity.Y *= 0.999f;

				Vector2 tipPos = GetTipPos();

				if (Collision.SolidCollision(tipPos - new Vector2(2), 4, 4))
				{
					Embedded = true;

					Collision.HitTiles(tipPos - new Vector2(2), Projectile.velocity, 4, 4);
				}
			}
			else
			{
				Projectile.velocity = Vector2.Zero;

				Timer++;

				if (Timer > 50)
				{
					if (Main.myPlayer == Projectile.owner)
					{
						int type = ModContent.ProjectileType<CrookLocust>();
						Vector2 position = GetTipPos(false);
						int proj = Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, -Vector2.UnitY, type, Projectile.damage, 0, Projectile.owner);
						Main.projectile[proj].localAI[0] = Projectile.whoAmI;
					}

					Timer = 0;
				}
			}
		}

		private Vector2 GetTipPos(bool anchor = true)
		{
			if (Embedded)
				return Projectile.Center + (Projectile.rotation + MathHelper.PiOver2 * 1.5f).ToRotationVector2() * 26 * (anchor ? 1 : -1);

			return Projectile.Center + Vector2.Normalize(Projectile.velocity) * 26 * (anchor ? 1 : -1);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) => !(Embedded = true);

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			SpriteEffects effect = Owner.direction == 1 && VisuallyHeld ? SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, 1f, effect, 0);
			return false;
		}
	}

	private class CrookLocust : ModProjectile
	{
		public NPC TargetNPC => Main.npc[Target];
		public Projectile ParentProj => Main.projectile[(int)ProjOwner];

		int Target
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		bool Init
		{
			get => Projectile.ai[1] == 1;
			set => Projectile.ai[1] = value ? 1 : 0;
		}

		ref float Timer => ref Projectile.ai[2];

		ref float ProjOwner => ref Projectile.localAI[0];

		public override void SetStaticDefaults() => ProjectileID.Sets.SentryShot[Type] = true;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.Size = new Vector2(12);
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 3;
		}

		public override void AI()
		{
			if (!Init)
			{
				Init = true;
				Target = -1;
			}

			Timer++;

			if (Target == -1)
			{
				List<int> options = [];

				foreach (NPC npc in Main.ActiveNPCs)
				{
					if (npc.CanBeChasedBy() && npc.DistanceSQ(Projectile.Center) < 500 * 500)
						options.Add(npc.whoAmI);
				}

				if (options.Count > 0 && Main.myPlayer == Projectile.owner)
				{
					Target = Main.rand.Next(options);
					Projectile.netUpdate = true;
					Timer = -1;
				}

				Timer++;

				if (Timer % 90 == 0)
				{

				}
			}
			else
			{
				if (!TargetNPC.CanBeChasedBy())
				{
					Target = -1;
					Timer = 0;
					return;
				}

				Timer++;

				if (Timer > 16)
				{
					Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.DirectionTo(TargetNPC.Center) * 8, 0.3f);

					if (Timer > 40)
						Timer = 0;
				}
				else
					Projectile.velocity *= 0.9f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) => false;
	}

	public override void SetDefaults()
	{
		Item.damage = 25;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.rare = ItemRarityID.Green;
		Item.DamageType = DamageClass.Summon;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<LocustCrookProjectile>();
		Item.shootSpeed = 12;
		Item.useStyle = ItemUseStyleID.RaiseLamp;
		Item.autoReuse = false;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.type == type && projectile.owner == player.whoAmI && projectile.ai[2] == 0)
			{
				(projectile.ModProjectile as LocustCrookProjectile).Throw();
				return false;
			}
		}

		return false;
	}

	public override void HoldItem(Player player)
	{
		if (Main.myPlayer != player.whoAmI || player.itemAnimation > 0)
			return;

		int type = ModContent.ProjectileType<LocustCrookProjectile>();

		foreach (Projectile projectile in Main.ActiveProjectiles)
			if (projectile.type == type && projectile.owner == player.whoAmI && projectile.ai[2] == 0)
				return;

		Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, type, Item.damage, 0, player.whoAmI);
	}
}
