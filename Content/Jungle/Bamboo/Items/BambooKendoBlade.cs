using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Jungle.Bamboo.Tiles;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Jungle.Bamboo.Items;

public class BambooKendoBlade : ModItem, IDashSword
{
	private float swingArc;
	private static Asset<Texture2D> HeldTexture;

	public override void SetStaticDefaults() => HeldTexture = ModContent.Request<Texture2D>(Texture + "Proj");

	public override void SetDefaults()
	{
		Item.damage = 12;
		Item.crit = 2;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 20;
		Item.DamageType = DamageClass.Melee;
		Item.width = Item.height = 46;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(silver: 3);
		Item.rare = ItemRarityID.White;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<KendoBladeLunge>();
		Item.shootSpeed = 1f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override void HoldItem(Player player)
	{
		if (!player.ItemAnimationActive)
		{
			player.GetModPlayer<DashSwordPlayer>().holdingSword = true;
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -1.1f * player.direction);
		}
	}

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
			Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<KendoBladeLunge>(), (int)(damage * 2.25f), knockback, player.whoAmI);
		else
		{
			float oldSwingArc = swingArc;
			while (oldSwingArc == swingArc) //Never select the same arc twice
				swingArc = Main.rand.NextFromList(3.14f, 1.7f, -1.5f, -4f, 2f);

			Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<KendoBladeSwing>(), damage, knockback, player.whoAmI, swingArc);
		}

		return false;
	}

	public void DrawHeld(ref PlayerDrawSet info)
	{
		if (!HeldTexture.IsLoaded)
			return;

		var texture = HeldTexture;

		var frame = texture.Frame(1, 5, 0, 4, 0, -2);
		var center = info.drawPlayer.MountedCenter;
		var drawPos = new Vector2((int)(center.X - Main.screenPosition.X), (int)(center.Y + 6 * info.drawPlayer.gravDir - Main.screenPosition.Y + info.drawPlayer.gfxOffY));

		float rotation = -.15f * info.drawPlayer.direction + info.drawPlayer.fullRotation + MathHelper.Pi;
		var color = Lighting.GetColor((int)info.drawPlayer.Center.X / 16, (int)info.drawPlayer.Center.Y / 16);

		info.DrawDataCache.Add(new DrawData(texture.Value, drawPos, frame, color, rotation, new Vector2(30), 1, info.playerEffect, 0));
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemMethods.AutoItemType<StrippedBamboo>(), 20).AddTile(TileID.WorkBenches).Register();
}

public class KendoBladeSwing : ModProjectile
{
	/// <summary> Damage distance in pixels. </summary>
	private const int Reach = 100;

	private float SwingTime => Main.player[Projectile.owner].itemTimeMax; //The full duration of the swing
	public ref float SwingArc => ref Projectile.ai[0]; //The full arc of the swing in radians
	public ref float Counter => ref Projectile.ai[1];

	public override string Texture => "SpiritReforged/Content/Jungle/Bamboo/Items/BambooKendoBladeProj";

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.BambooKendoBlade.DisplayName");

	public override void SetStaticDefaults() => Main.projFrames[Type] = 5;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(18);
		Projectile.DamageType = DamageClass.Melee;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.scale = .5f;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		float easeMult = .3f; //Controls how prominent easing is

		Projectile.spriteDirection = Projectile.direction = owner.direction = Projectile.velocity.X > 0 ? 1 : -1;

		float ease = Common.Easing.EaseFunction.EaseCircularOut.Ease(MathHelper.Min((float)Counter / (SwingTime * easeMult), 1));
		float progress = Projectile.direction == -1 ? 1f - ease : ease;

		Projectile.rotation = Projectile.velocity.ToRotation() - SwingArc / 2 + SwingArc * progress;
		Projectile.Center = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
		owner.heldProj = Projectile.whoAmI;

		if (++Counter < SwingTime - 2)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

		Projectile.scale = MathHelper.Min(Projectile.scale + .075f, 1); //Fade in
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int lineWidth = 30;
		var endPos = Projectile.Center + (Vector2.UnitX * (Reach - lineWidth)).RotatedBy(Projectile.rotation);
		float collisionPoint = 0f;

		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, endPos, lineWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		int swingDir = Math.Sign(SwingArc);
		float visCounter = MathHelper.Min((float)Counter / (SwingTime / 2), 1);

		var texture = TextureAssets.Projectile[Type];
		var effects = Projectile.spriteDirection * swingDir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
		var frame = texture.Frame(1, Main.projFrames[Type], 0, (int)(visCounter * (Main.projFrames[Type] - 1)), 0, -2);
		var origin = new Vector2(4, effects == SpriteEffects.FlipVertically ? 9 : 30); //The handle
		var position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
		float afterimageLength = 1f * (1f - visCounter);

		lightColor = Color.Lerp(lightColor.MultiplyRGB(Color.LightGray), lightColor, visCounter);

		for (int i = 3; i > 0; i--)
		{
			var color = Projectile.GetAlpha(lightColor) * (1f - (float)i / Projectile.oldPos.Length);
			float rotation = Projectile.rotation - afterimageLength * i / 5f * Projectile.spriteDirection * swingDir;

			Main.EntitySpriteDraw(texture.Value, position, frame, color, rotation, origin, Projectile.scale, effects);
		}

		DrawSmear(lightColor, effects);
		return false;
	}

	private void DrawSmear(Color lightColor, SpriteEffects effects)
	{
		Main.instance.LoadProjectile(985);
		var smear = TextureAssets.Projectile[985].Value;
		var player = Main.player[Projectile.owner];

		int frame = (int)(Counter / SwingTime * 12f);
		var source = smear.Frame(1, 4, 0, frame);
		var color = Projectile.GetAlpha(lightColor) * Math.Min(Counter / SwingTime * 3, 1) * .5f;

		float rotation = Projectile.rotation - SwingArc * .5f * Projectile.spriteDirection;
		float dist = Reach - 20;
		var position = player.Center + (Vector2.UnitX * dist).RotatedBy(rotation) - Main.screenPosition;

		Main.EntitySpriteDraw(new DrawData(smear, position, source, color, rotation, new Vector2(source.Width, source.Height / 2), .75f, effects, 0));
	}
}

public class KendoBladeLunge : ModProjectile
{
	public static readonly SoundStyle BigSwing = new("SpiritReforged/Assets/SFX/Item/BigSwing");

	public int targetWhoAmI = -1;
	private Vector2 lastPosition;

	private const int DashDuration = 20;
	private const int StrikeDelay = 20;
	private const int FlourishTime = 10;

	public ref float Counter => ref Projectile.ai[0];

	public override string Texture => "SpiritReforged/Content/Jungle/Bamboo/Items/BambooKendoBladeProj";
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.BambooKendoBlade.DisplayName");

	public override void SetStaticDefaults() => Main.projFrames[Type] = 5;
	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(38);
		Projectile.DamageType = DamageClass.Melee;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.frame = Main.projFrames[Type] - 1;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		var mp = owner.GetModPlayer<DashSwordPlayer>();
		mp.SetDashCooldown(40);

		if (Counter < DashDuration) //Ongoing dash
		{
			mp.dashing = true;
			float quote = Counter / DashDuration;

			if (Counter + 1 == DashDuration)
				Projectile.scale = 1.5f;
			if (Counter > DashDuration - 4)
				owner.velocity *= .5f;
			else
			{
				int magnitude = 20;
				owner.velocity = Vector2.Lerp(owner.velocity, Projectile.velocity * magnitude * 2, quote * quote * quote * quote);

				if (Counter == DashDuration / 2)
				{
					owner.velocity = Projectile.velocity * magnitude;
					SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
				}

				if (Counter == 0)
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1 }, Projectile.Center);
			}

			owner.velocity.Y += owner.gravity * 8 * quote;
			owner.armorEffectDrawShadow = true;
			owner.armorEffectDrawShadowLokis = true;

			if (Projectile.owner == Main.myPlayer && targetWhoAmI == -1) //Find a target
			{
				if (lastPosition == Vector2.Zero)
					lastPosition = owner.Center;

				float collisionPoint = 0;
				var crossed = Main.npc.Where(x => x.CanBeStruck() && Collision.CheckAABBvLineCollision(x.Hitbox.TopLeft(), x.Hitbox.Size(), lastPosition, owner.Center, 15, ref collisionPoint)).OrderBy(x => x.Distance(lastPosition)).FirstOrDefault();

				if (crossed != default)
					targetWhoAmI = crossed.whoAmI;
			}

			//Spawn dusts
			var dust = Dust.NewDustDirect(owner.position, owner.width, owner.height, DustID.Smoke, Alpha: 100, Scale: (1f - quote) * 2f);
			dust.velocity = (owner.velocity * Main.rand.NextFloat(.2f)).RotatedByRandom(.5f);
			dust.noGravity = true;
			dust.noLightEmittence = true;
		}
		else
			mp.dashing = false;

		if (Counter > DashDuration + StrikeDelay)
		{
			Projectile.frame = (int)(Projectile.localAI[0] / FlourishTime * Main.projFrames[Type]);
			Projectile.localAI[0]++;
		}

		Projectile.spriteDirection = Projectile.direction = owner.direction = Projectile.velocity.X > 0 ? 1 : -1;
		Projectile.scale = MathHelper.Max(Projectile.scale - .05f, 1);

		if (Counter < DashDuration / 1.5f)
		{
			Projectile.Center = owner.MountedCenter + Projectile.velocity;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi - .4f * Projectile.direction;
		}
		else
		{
			float quote = Projectile.localAI[0] / FlourishTime;

			Projectile.Center = owner.MountedCenter + Projectile.velocity * (25 - 25 * quote);
			Projectile.rotation = AngleLerp(owner, Projectile.velocity.ToRotation(), Projectile.direction == 1 ? MathHelper.Pi : 0, quote);
		}

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, owner.AngleTo(Projectile.Center) - 1.57f);
		owner.heldProj = Projectile.whoAmI;

		if (++Counter < DashDuration + StrikeDelay + FlourishTime)
			owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

		if (Counter == 1)
			lastPosition = owner.Center;

		static float AngleLerp(Player player, float curAngle, float targetAngle, float amount) //Modified Utils.AngleLerp with more control over direction
		{
			float angle;
			if (targetAngle < curAngle)
			{
				float num = targetAngle + (float)Math.PI * 2f;
				angle = MathHelper.Lerp(curAngle, targetAngle, amount);
			}
			else
			{
				if (!(targetAngle > curAngle))
					return curAngle;

				float num = targetAngle - (float)Math.PI * 2f;
				angle = player.direction == -1 ? MathHelper.Lerp(curAngle, num, amount) : MathHelper.Lerp(curAngle, targetAngle, amount);
			}

			return MathHelper.WrapAngle(angle);
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		for (int i = 0; i < 20; i++)
		{
			float magnitude = Main.rand.NextFloat();

			var dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f), DustID.RainCloud, Projectile.velocity * 8 * magnitude, Alpha: 120, Scale: 2f * (1f - magnitude));
			dust.noGravity = true;
		}

		SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = .8f }, Projectile.Center);
		SoundEngine.PlaySound(BigSwing, target.Center);
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (CanDamage() is true)
		{
			if (targetWhoAmI != -1 && Main.npc[targetWhoAmI] is NPC target && targetHitbox == target.Hitbox)
				return true;
		}

		return false;
	}

	public override bool? CanDamage() => Counter == DashDuration + StrikeDelay;

	public override bool PreDraw(ref Color lightColor)
	{
		float Seed(float mult = 0) => (lastPosition.X + lastPosition.Y * mult) * .05f % 1;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
		var frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		var origin = new Vector2(30, effects == SpriteEffects.FlipVertically ? 9 : 30); //The handle

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);

		#region trail
		var owner = Main.player[Projectile.owner];

		Texture2D trail = TextureAssets.Projectile[607].Value;
		float opacity = 1f - Counter / DashDuration;
		float angle = owner.AngleTo(lastPosition);

		for (int i = 0; i < 2 + (int)(Seed() * 3f); i++)
		{
			var position = owner.Center + new Vector2(10 * Seed(i), 24 * Seed(i) - 12).RotatedBy(angle) - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
			var scale = new Vector2(.2f + Seed(i) * .2f, 1f / trail.Height * owner.Distance(lastPosition) * (1f - Seed(i) * .2f));
			var color = (Color.Lerp(lightColor, lightColor.MultiplyRGB(Color.SlateGray), Seed(i)) with { A = 0 }) * opacity;

			Main.EntitySpriteDraw(trail, position, null, color, angle + MathHelper.PiOver2, trail.Frame().Bottom(), scale, SpriteEffects.None, 0);
		}

		trail = TextureAssets.Projectile[874].Value;
		Main.EntitySpriteDraw(trail, owner.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), null, (lightColor with { A = 0 }) * opacity, 
			angle, trail.Frame().Left(), new Vector2(1f / trail.Width * owner.Distance(lastPosition), .25f), SpriteEffects.None, 0);
		#endregion

		return false;
	}
}
