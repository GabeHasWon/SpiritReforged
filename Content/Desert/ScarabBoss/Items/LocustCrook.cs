using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

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
			Projectile.sentry = true;
			Projectile.velocity = Projectile.DirectionTo(Main.MouseWorld) * 12;
			Owner.UpdateMaxTurrets();

			Held = false;
			Rotation = Projectile.rotation + MathHelper.PiOver2 * 1.5f;
			ThrowTimer = 30;
		}

		public override void AI()
		{
			if (!Held && ThrowTimer > 0)
			{
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, Rotation + ThrowTimer * 0.03f);
				ThrowTimer--;
			}

			if (Held)
			{
				Vector2 mouse = PlayerMouseHandler.GetMouse(Projectile.owner);
				Owner.ChangeDir(Math.Sign(mouse.X - Owner.Center.X));

				Projectile.rotation = Projectile.AngleTo(mouse) - MathHelper.PiOver2 * 1.5f;
				Projectile.Center = Owner.Center + Projectile.DirectionTo(mouse).RotatedBy(Owner.direction == 1 ? -MathHelper.PiOver2 : MathHelper.PiOver2) * 14;

				if (Owner.direction == 1)
					Projectile.rotation -= MathHelper.Pi;

				Owner.heldProj = Projectile.whoAmI;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + MathHelper.PiOver2 * 1.5f);

				if (Owner.HeldItem.type != ModContent.ItemType<LocustCrook>())
				{
					Projectile.Kill();
					return;
				}

				return;
			}

			if (!Embedded)
			{
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
						Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, -Vector2.UnitY, type, Projectile.damage, 0, Projectile.owner);
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
			SpriteEffects effect = Owner.direction == 1 && Held ? SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, 1f, effect, 0);
			return false;
		}
	}

	private class CrookLocust : ModProjectile
	{
		public NPC TargetNPC => Main.npc[Target];

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

		public override void SetStaticDefaults() => ProjectileID.Sets.SentryShot[Type] = true;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.Size = new Vector2(12);
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
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
				}
			}
			else
			{
				if (!TargetNPC.CanBeChasedBy())
				{
					Target = -1;
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
		if (Main.myPlayer != player.whoAmI)
			return;

		int type = ModContent.ProjectileType<LocustCrookProjectile>();

		foreach (Projectile projectile in Main.ActiveProjectiles)
			if (projectile.type == type && projectile.owner == player.whoAmI && projectile.ai[2] == 0)
				return;

		Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, type, Item.damage, 0, player.whoAmI);
	}
}
