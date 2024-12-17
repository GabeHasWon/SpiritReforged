﻿using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Blunderbuss;

public class Blunderbuss : ModItem
{
	public override void SetDefaults()
    {
        Item.width = Item.height = 12;
        Item.damage = 5;
        Item.knockBack = 3.5f;
        Item.useAnimation = Item.useTime = 80;
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Cannon_1") with { PitchVariance = .35f };
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.rare = ItemRarityID.Blue;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.DamageType = DamageClass.Ranged;
        Item.shoot = ProjectileID.Bullet;
        Item.useAmmo = AmmoID.Bullet;
        Item.shootSpeed = 10f;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		const float spread = .4f; //In radians
		const float speedVariance = .5f;

        var unit = Vector2.Normalize(velocity);
		float fxDistance = 30;

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Cannon_2") with { Volume = .75f, Pitch = .5f }, position);

		for (int i = 0; i < 10; i++)
            Dust.NewDustPerfect(position + unit * fxDistance + Main.rand.NextVector2Unit() * Main.rand.NextFloat(12f), 
				DustID.Torch, unit * Main.rand.NextFloat(), 0, default, Main.rand.NextFloat(2f)).noGravity = true;
        for (int i = 0; i < 15; i++)
            Dust.NewDustPerfect(position + unit * fxDistance + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), 
				DustID.Smoke, unit.RotatedByRandom(1f) * Main.rand.NextFloat(), 240, default, Main.rand.NextFloat(5f, 8f));

		player.velocity -= velocity * .15f; //Player knockback

		//Spawn a harmless animated projectile
		Projectile.NewProjectile(source, position, unit, ModContent.ProjectileType<BlunderbussProj>(), 0, 0, player.whoAmI);

		for (int i = 0; i < 5; i++)
		{
			var shot = Projectile.NewProjectileDirect(source, position, velocity.RotatedByRandom(spread) * Main.rand.NextFloat(1f - speedVariance, 1f + speedVariance), type, damage, knockback, player.whoAmI);

			if (shot.TryGetGlobalProjectile(out BlunderbussProjectile bProj))
			{
				bProj.firedFromBlunderbuss = true;
				shot.scale = Main.rand.NextFloat(.25f, 1f);
				shot.timeLeft = BlunderbussProjectile.timeLeftMax; //Shorten lifespan
				shot.netUpdate = true; //Sync all changes made after NewProjectileDirect was called

				for (int d = 0; d < 3; d++)
					Dust.NewDustPerfect(position + unit * fxDistance, DustID.Torch, shot.velocity * Main.rand.NextFloat(.5f, 1.5f)).noGravity = true;
			}
		}

		return false;
    }
}

public class BlunderbussProj : ModProjectile
{
    public float GetFeedback()
	{
		var owner = Main.player[Projectile.owner];
		const int feedbackLength = 12; //For how long the gun receives shot feedback

		return MathHelper.Clamp(((float)owner.itemTime 
			- (owner.itemTimeMax - feedbackLength)) / (owner.itemTimeMax - (owner.itemTimeMax - feedbackLength)), 0, 1);
	}

    public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.Blunderbuss.DisplayName");
	public override string Texture => base.Texture.Replace("Proj", string.Empty);

	public override void SetDefaults()
    {
        Projectile.Size = new Vector2(12);
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
		var owner = Main.player[Projectile.owner];
		float holdDistance = -8 - GetFeedback() * 10; //How far the projectile is held from the player center
		float frontArmRotation = .4f;

		Projectile.timeLeft = Math.Min(Projectile.timeLeft, owner.itemTimeMax); //Set our max time here because we can't do it in defaults
		Player.CompositeArmStretchAmount armStretch = (owner.itemTimeMax - Projectile.timeLeft) switch
		{
			0 => Player.CompositeArmStretchAmount.Full,
			1 => Player.CompositeArmStretchAmount.ThreeQuarters,
			2 => Player.CompositeArmStretchAmount.Quarter,
			_ => Player.CompositeArmStretchAmount.None
		};

		if (Projectile.timeLeft == 45)
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/ClickClack") with { Volume = .25f, Pitch = -.5f }, Projectile.Center);
		else if (Projectile.timeLeft == 20)
			SoundEngine.PlaySound(SoundID.Unlock with { Volume = .5f, Pitch = -.2f }, Projectile.Center);

		if (Projectile.timeLeft < 50)
		{
			armStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
			if (Projectile.timeLeft < 20)
				armStretch = Player.CompositeArmStretchAmount.None;

			frontArmRotation = -.2f;

			if (Main.rand.NextBool())
			{
				Dust.NewDustPerfect(Projectile.Center + Vector2.Normalize(Projectile.velocity) * 38 + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), 
					DustID.Smoke, Vector2.UnitY * -Main.rand.NextFloat(), 230, default, Main.rand.NextFloat(2.5f, 4f));
			}
		}

		Projectile.Center = owner.Center + Projectile.velocity * holdDistance;
        Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0) ? 1 : -1;
        Projectile.rotation = Projectile.velocity.ToRotation() + (float)(Math.Sin(GetFeedback() * 4f) * .25f) * -owner.direction;

		owner.heldProj = Projectile.whoAmI;
		owner.SetCompositeArmFront(true, armStretch, Projectile.rotation - 1.57f + frontArmRotation * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f);
	}

    public override bool PreDraw(ref Color lightColor)
    {
		var texture = TextureAssets.Projectile[Type].Value;

		var randomizer = Main.gamePaused ? Vector2.Zero : Main.rand.NextVector2Unit();
		var pos = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY) + randomizer * GetFeedback() * 1.5f;
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

        Main.EntitySpriteDraw(texture, pos, null, Projectile.GetAlpha(lightColor),
            Projectile.rotation, texture.Frame().Left(), Projectile.scale, effects);

        DrawMuzzleFlash();
        return false;
    }

    private void DrawMuzzleFlash()
    {
        var texture = ModContent.Request<Texture2D>(Texture + "_Flash").Value;
		var pos = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY) 
			+ (Vector2.UnitX * (TextureAssets.Projectile[Type].Width() - Projectile.width / 2 - 4)).RotatedBy(Projectile.velocity.ToRotation());

		int frame = (int)((Main.player[Projectile.owner].itemTimeMax - Projectile.timeLeft) / 2f);
		var source = texture.Frame(1, 6, 0, frame, 0, -2);

        Main.EntitySpriteDraw(texture, pos, source, Projectile.GetAlpha(Color.White with { A = 150 }), Projectile.velocity.ToRotation(), new Vector2(0, source.Height / 2), Projectile.scale, SpriteEffects.None);
	}

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;
}
