using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Items.HuntingRifle;

public class HuntingRifle : ModItem
{
	public static Asset<Texture2D> CursorTexture { get; private set; }
	public static readonly SoundStyle Gunshot = new("SpiritReforged/Assets/SFX/Item/Gunshot");

	/// <summary> The item type of <see cref="HuntingRifle"/> cached for detour efficiency. </summary>
	private static int StaticType;
	private static float CursorOpacity;

	public override void Load()
	{
		if (!Main.dedServ)
			CursorTexture = DrawHelpers.RequestLocal(GetType(), "Cursor_Reticle", false);

		CustomCursor.DrawCustomCursor += DrawCustomCursor;
	}

	private static void DrawCustomCursor(bool thick)
	{
		if (Main.gameMenu || Main.LocalPlayer.mouseInterface || Main.LocalPlayer.HeldItem.type != StaticType)
		{
			CursorOpacity = 0;
			return;
		}

		float scale = Main.cursorScale * .6f * (thick ? 1.1f : 1f);
		float distance = MathHelper.Clamp(Main.LocalPlayer.Distance(Main.MouseWorld) / HunterGlobalProjectile.maxRange, 0, 1);
		float offsetLength = 5f + (1f - distance) * 5f
			+ (float)Main.LocalPlayer.itemAnimation / Main.LocalPlayer.itemAnimationMax * 3f
			+ MathHelper.Min(Main.LocalPlayer.velocity.Length(), 2);
		//Distance, item animation, and player velocity adjustments

		CursorOpacity = MathHelper.Min(CursorOpacity + .05f, 1f);
		Color color = Main.cursorColor;

		if (thick) //Border cursor color
			color = Main.MouseBorderColor;
		else if (!Main.gameMenu && Main.LocalPlayer.hasRainbowCursor) //Rainbow cursor color
			color = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.25f % 1f, 1f, 0.5f);

		for (int c = 0; c < 8; c++)
		{
			var frame = CursorTexture.Frame(2, 2, 0, thick ? 1 : 0, -2, -2);
			var origin = frame.Size() / 2;
			float rotation = MathHelper.PiOver2 * c;
			var position = Main.MouseScreen + new Vector2(1, -1).RotatedBy(rotation) * offsetLength;

			if (c > 3)
			{
				frame = CursorTexture.Frame(2, 2, 1, thick ? 1 : 0, -2, -2);
				position = Main.MouseScreen + new Vector2(1, 0).RotatedBy(rotation) * (offsetLength + 4);
			}

			if (!thick)
			{
				var shadowColor = color.MultiplyRGB(new Color(100, 100, 100)) * .25f;
				Main.spriteBatch.Draw(CursorTexture.Value, position + new Vector2(2), frame, shadowColor, rotation, origin, scale, SpriteEffects.None, 0f);
			}

			Main.spriteBatch.Draw(CursorTexture.Value, position, frame, color * CursorOpacity, rotation, origin, scale, SpriteEffects.None, 0f);
		}
	}

	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<WrithingSticks.WrithingSticks>();
		StaticType = Type;
	}

	public override void SetDefaults()
    {
        Item.width = Item.height = 12;
        Item.damage = 22;
        Item.knockBack = 5;
        Item.useAnimation = Item.useTime = 60;
		Item.UseSound = Gunshot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.rare = ItemRarityID.Blue;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.DamageType = DamageClass.Ranged;
        Item.shoot = ProjectileID.Bullet;
        Item.useAmmo = AmmoID.Bullet;
        Item.shootSpeed = 10f;
		Item.value = Item.sellPrice(0, 3, 0, 0);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        var unit = Vector2.Normalize(velocity);
		float fxDistance = 45;

		SoundEngine.PlaySound(SoundID.Item100 with { Volume = .6f, PitchVariance = .2f, Pitch = 1f }, position);
		ParticleHandler.SpawnParticle(new StarParticle(position + unit * (fxDistance - 8), Vector2.Zero, Color.Goldenrod, .5f, 2, 0));

		for (int i = 0; i < 10; i++)
            Dust.NewDustPerfect(position + unit * fxDistance + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), 
				DustID.Torch, unit * Main.rand.NextFloat(), 0, default, Main.rand.NextFloat(2f)).noGravity = true;
        for (int i = 0; i < 5; i++)
            Dust.NewDustPerfect(position + unit * fxDistance + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f), 
				DustID.Smoke, unit * Main.rand.NextFloat(), 200, default, Main.rand.NextFloat(3f));

		float mult = 1f;
		if (player.velocity == Vector2.Zero)
		{
			ParticleHandler.SpawnParticle(new SmokeCircleParticle(position + unit * fxDistance, unit * -.2f, 
				Lighting.GetColor(position.ToTileCoordinates(), Color.LightSlateGray), .5f, unit.ToRotation(), 30));

			//Grant a damage bonus (+25%) when standing still. Additional bonuses are applied in HunterGlobalProjectile
			mult = 1.25f;
		}

		//Spawn a harmless animated projectile
		Projectile.NewProjectile(source, position, unit, ModContent.ProjectileType<HuntingRifleProj>(), 0, 0, player.whoAmI);

		//Spawn a damaging projectile
		PreNewProjectile.New(source, position, velocity, type, (int)(damage * mult), knockback, player.whoAmI, preSpawnAction: (Projectile projectile) =>
		{
			if (projectile.TryGetGlobalProjectile(out HunterGlobalProjectile hunter))
			{
				hunter.firedFromHuntingRifle = true;
			}
		});

		return false;
    }
}

public class HuntingRifleProj : ModProjectile
{
	public static readonly SoundStyle Eject = new("SpiritReforged/Assets/SFX/Item/Eject")
	{
		PitchVariance = .2f
	};

	public static readonly SoundStyle Ring = new("SpiritReforged/Assets/SFX/Item/Ring")
	{
		PitchVariance = .25f,
		Pitch = -.6f
	};

	public static readonly Asset<Texture2D> Flash = ModContent.Request<Texture2D>(ModContent.GetInstance<HuntingRifleProj>().Texture + "_Flash");

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.HuntingRifle.DisplayName");
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
		float holdDistance = -14 - GetFeedback() * 10; //How far the projectile is held from the player center
		int halfTime = owner.itemTimeMax / 2;

		Projectile.timeLeft = Math.Min(Projectile.timeLeft, owner.itemTimeMax); //Set our max time here because we can't do it in defaults
		Player.CompositeArmStretchAmount armStretch = (GetFeedback() * 4) switch
		{
			0 => Player.CompositeArmStretchAmount.Full,
			1 => Player.CompositeArmStretchAmount.ThreeQuarters,
			2 => Player.CompositeArmStretchAmount.Quarter,
			_ => Player.CompositeArmStretchAmount.None
		};

		if (Projectile.timeLeft == halfTime + 15)
			SoundEngine.PlaySound(Eject, Projectile.Center);
		else if (Projectile.timeLeft == halfTime)
		{
			var velocity = (new Vector2(-owner.direction, -owner.gravDir) * Main.rand.NextFloat(8f, 12f)).RotateRandom(.2f);
			Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<Dusts.ShellDust>(), velocity).scale = 1; //Scale is slightly randomized otherwise

			SoundEngine.PlaySound(Ring, Projectile.Center);
		}
		else if (Projectile.timeLeft < halfTime)
		{
			armStretch = Player.CompositeArmStretchAmount.Quarter;
			holdDistance -= MathHelper.Clamp(((float)Projectile.timeLeft - (halfTime - 8)) / (halfTime - (halfTime - 8)), 0, 1) * 3f;
		}

		Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter + Projectile.velocity * holdDistance);
        Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0) ? 1 : -1;
        Projectile.rotation = Projectile.velocity.ToRotation() + GetFeedback() * .15f * -owner.direction;

		owner.heldProj = Projectile.whoAmI;
		owner.SetCompositeArmFront(true, armStretch, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + 1f * owner.direction);
	}

	public float GetFeedback()
	{
		var owner = Main.player[Projectile.owner];
		const int feedbackLength = 5; //For how long the gun receives shot feedback

		return MathHelper.Clamp(((float)owner.itemTime
			- (owner.itemTimeMax - feedbackLength)) / (owner.itemTimeMax - (owner.itemTimeMax - feedbackLength)), 0, 1);
	}

	public override bool PreDraw(ref Color lightColor)
    {
		var texture = TextureAssets.Projectile[Type].Value;

		var randomizer = Main.gamePaused ? Vector2.Zero : Main.rand.NextVector2Unit();
		var pos = Projectile.Center - Main.screenPosition + randomizer * GetFeedback() * 1.5f;
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

        Main.EntitySpriteDraw(texture, pos, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Frame().Left(), Projectile.scale, effects);
        if (Projectile.timeLeft >= Main.player[Projectile.owner].itemTimeMax - 2)
            DrawMuzzleFlash();
        return false;
    }

    private void DrawMuzzleFlash()
    {
        var texture = Flash.Value;
        var pos = Projectile.Center - Main.screenPosition + (Vector2.UnitX * (TextureAssets.Projectile[Type].Width() - Projectile.width / 2)).RotatedBy(Projectile.velocity.ToRotation());
        float unit = Main.rand.NextFloat(.5f);
        var scale = new Vector2(1 + unit, 1 - unit) * Projectile.scale;

        Main.EntitySpriteDraw(texture, pos, null, Projectile.GetAlpha(Color.White with { A = 150 }), Projectile.velocity.ToRotation(), texture.Frame().Left(), scale, SpriteEffects.None);
	}

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;
}
