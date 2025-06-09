using ReLogic.Utilities;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Snow.Frostbite;

[FromClassic("HowlingScepter")]
public class FrostbiteItem : ModItem
{
	public const int AttackRange = 200;

	public override void SetStaticDefaults()
	{
		DiscoveryHelper.RegisterPickup(Type, new SoundStyle("SpiritReforged/Assets/SFX/Ambient/MagicFeedback1"));

		MoRHelper.AddElement(Item, MoRHelper.Wind);
		MoRHelper.AddElement(Item, MoRHelper.Ice, true);
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 24;
		Item.damage = 8;
		Item.knockBack = 0;
		Item.DamageType = DamageClass.Magic;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.channel = true;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.value = Item.sellPrice(0, 0, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item20;
		Item.mana = 2;
		Item.shootSpeed = AttackRange;
		Item.shoot = ModContent.ProjectileType<FrostbiteProj>();
	}
}

public class FrostbiteProj : ModProjectile
{
	private const int FadeTime = 20;

	public ref float Counter => ref Projectile.ai[0];
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.Frostbite.DisplayName");

	private SlotId loopedSound = SlotId.Invalid;

	public override void SetStaticDefaults() => Main.projFrames[Type] = 2;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(20);
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.ArmorPenetration = 2;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.Opacity = 0;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		if (Projectile.timeLeft > FadeTime && !Main.dedServ)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/PageFlip") with { PitchVariance = 0.3f, Volume = 0.65f }, owner.Center);
			SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = .5f }, owner.Center);
			SoundEngine.PlaySound(SoundID.AbigailAttack with { Volume = .15f, Pitch = 1f }, owner.Center);
			Projectile.timeLeft = FadeTime;
		}

		Projectile.Opacity = MathHelper.Min(Projectile.Opacity + 1f / FadeTime, 1);

		owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation() + (Projectile.direction < 0 ? MathHelper.Pi : 0));
		owner.heldProj = Projectile.whoAmI;

		if (Projectile.owner == Main.myPlayer)
		{
			var oldVelocity = Projectile.velocity;
			Projectile.velocity = new Vector2(GetVelocityLength(), 0).RotatedBy(owner.AngleTo(Main.MouseWorld));

			if (Projectile.velocity != oldVelocity)
				Projectile.netUpdate = true;
		}

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		Projectile.rotation = .4f * owner.direction;
		Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter + new Vector2(owner.direction * 14, 4));

		float rotation = Projectile.rotation - 1.57f * Projectile.direction;
		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);
		owner.ChangeDir(Projectile.velocity.X < 0 ? -1 : 1);
		owner.itemTime = owner.itemAnimation = FadeTime;

		if (owner.channel && DrainMana(owner, 15))
			Projectile.timeLeft = FadeTime;

		Projectile.UpdateFrame(5, 1);
		DoVisuals();
		UpdateSound();

		Counter++;
	}

	/// <summary> Drains <paramref name="owner"/>'s mana by <paramref name="manaPerSecond"/>. </summary>
	/// <returns> Whether mana was drained (was above zero). </returns>
	private bool DrainMana(Player owner, int manaPerSecond)
	{
		if (Counter % (60f / manaPerSecond) == 0)
			owner.statMana = Math.Max(owner.statMana - 1, 0);

		return owner.statMana > 0;
	}

	private void DoVisuals()
	{
		if (Main.rand.NextBool(10))
		{
			var dust = Dust.NewDustDirect(Projectile.position - new Vector2(0, Projectile.height / 2), Projectile.width, Projectile.height, DustID.GemSapphire);
			dust.noGravity = true;
			dust.velocity = new Vector2(0, -Main.rand.NextFloat(2f));
		}

		var color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(.75f)).Additive(180);
		ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, Projectile.velocity * .08f, color * .3f, Main.rand.NextFloat(.25f), Common.Easing.EaseFunction.EaseCircularOut, 60));

		if (Main.rand.NextBool())
		{
			var pos = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f);
			var vel = (Projectile.velocity * Main.rand.NextFloat(.02f, .075f)).RotatedByRandom(.25f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, Color.White, Color.CornflowerBlue, Main.rand.NextFloat(0.15f, 0.45f), Main.rand.Next(30, 50), 1, delegate (Particle p)
			{
				p.Velocity *= .95f;
			}));
		}

		if (Main.rand.NextBool(10))
		{
			float mag = Main.rand.NextFloat();
			var pos = Projectile.Center + Projectile.velocity * mag * 1.1f;
			var vel = Vector2.Normalize(Projectile.velocity) * .5f;

			var p = new TexturedPulseCircle(pos, Color.White * (1f - mag) * .75f, Color.Blue, .5f, MathHelper.Lerp(300, 20, mag), 60, "Star", new Vector2(Main.rand.NextFloat(.25f, 2f)), Common.Easing.EaseFunction.EaseCubicOut).WithSkew(.75f, Projectile.velocity.RotatedByRandom(.25f).ToRotation());
			p.Velocity = vel;
			ParticleHandler.SpawnParticle(p);
		}

		if (Main.rand.NextBool(8))
		{
			var pos = Projectile.Center + (Projectile.velocity * Main.rand.NextFloat()).RotatedByRandom(.5f);
			var vel = Vector2.Normalize(Projectile.velocity).RotatedByRandom(1) * .25f;

			var p = new StarParticle(pos, vel, Color.White, Color.Blue * .25f, Main.rand.NextFloat(.03f, .1f), 60, 0);
			ParticleHandler.SpawnParticle(p);
		}

		if (Counter % 25 == 0)
		{
			float mag = Main.rand.NextFloat();
			var pos = Projectile.Center;
			var vel = (Projectile.velocity * Main.rand.NextFloat(.01f, .03f)).RotatedByRandom(.25f);
			var startColor = (Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(.75f)) * mag * .5f).Additive();

			var p = new SnowflakeParticle(pos, vel, startColor, (Color.RoyalBlue * mag).Additive(), Main.rand.NextFloat(.5f), 60, 0, Main.rand.Next(3), delegate (Particle p)
			{
				p.Velocity *= .98f;
				p.Velocity = p.Velocity.RotatedByRandom(.05f);
				p.Rotation += p.Velocity.Length() * .025f;
			});
			ParticleHandler.SpawnParticle(p);
		}

		if (Main.timeForVisualEffects % 10 == 0)
			ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Projectile.velocity.RotatedByRandom(.5f) * .05f, Color.White * .75f, new Vector2(.12f, .4f), 20));
	}

	private void UpdateSound()
	{
		const int volume = 1;

		Player owner = Main.player[Projectile.owner];
		if (Projectile.timeLeft >= FadeTime)
		{
			if (!SoundEngine.TryGetActiveSound(loopedSound, out ActiveSound sound))
				loopedSound = SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/Blizzard_Loop") with { Volume = volume, Pitch = -.6f, MaxInstances = 1, IsLooped = true }, Projectile.Center);
			else
			{
				sound.Position = Projectile.Center;
				sound.Pitch = (float)Math.Sin((float)(Main.timeForVisualEffects / 100f) * Math.PI) * .1f;
			}
		}
		else if (SoundEngine.TryGetActiveSound(loopedSound, out ActiveSound sound))
		{
			sound.Volume = MathHelper.Lerp(0, volume, (float)(Projectile.timeLeft - 1) / FadeTime);
			if (sound.Volume == 0)
			{
				sound.Stop();
				loopedSound = SlotId.Invalid;
			}
		}
	}

	private int GetVelocityLength()
	{
		const int sampleLength = 16;

		var start = Projectile.Center;
		var end = Projectile.Center + Vector2.Normalize(Projectile.velocity) * FrostbiteItem.AttackRange;
		int sampleCount = (int)(start.Distance(end) / sampleLength);

		for (int i = 0; i < sampleCount; i++)
		{
			float progress = (float)i / sampleCount;
			var sample = Vector2.Lerp(start, end, progress);

			if (WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(sample)))
				return (int)(FrostbiteItem.AttackRange * progress);
		}

		return FrostbiteItem.AttackRange;
	}

	public override void OnKill(int timeLeft)
	{
		if (SoundEngine.TryGetActiveSound(loopedSound, out ActiveSound sound)) //Failsafe
		{
			sound.Stop();
			loopedSound = SlotId.Invalid;
		}
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		const int width = 30;
		float collisionPoint = 0;

		var end = Projectile.Center + Projectile.velocity;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, width, ref collisionPoint);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<Frozen>(), 300);

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle drawFrame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, drawFrame, Projectile.GetAlpha(lightColor), Projectile.rotation, drawFrame.Size() / 2, Projectile.scale, effects, 0);

		Main.instance.LoadProjectile(917);
		Texture2D cursor = TextureAssets.Projectile[917].Value;
		float scalar = 1f - Projectile.Opacity;

		Main.EntitySpriteDraw(cursor, Projectile.Center - Main.screenPosition, null, (Color.White * scalar).Additive(), 0, cursor.Size() / 2, Projectile.Opacity, effects, 0);

		return false;
	}

	public override bool? CanCutTiles() => false;
}
