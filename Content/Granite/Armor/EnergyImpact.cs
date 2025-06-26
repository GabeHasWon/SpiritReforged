using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Granite.Armor;

/// <summary> The stationary component of <see cref="EnergyPlunge"/>. </summary>
public class EnergyImpact : ModProjectile, IDrawOverTiles
{
	public const int TimeLeftMax = 180;

	public override string Texture => AssetLoader.EmptyTexture;

	public ref float Strength => ref Projectile.ai[0];
	public bool Damaging => Projectile.timeLeft > TimeLeftMax - 2;

	public override void SetDefaults()
	{
		Projectile.Size = new(150);
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.timeLeft = TimeLeftMax;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == TimeLeftMax)
		{
			Projectile.rotation = Main.rand.NextFloat(MathHelper.Pi);
			Projectile.scale = Strength;
		}
	}

	public override bool? CanDamage() => Damaging ? null : false;
	public override bool? CanCutTiles() => Damaging ? null : false;
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = target.Center.X - Projectile.Center.X < 0 ? -1 : 1;
	public override bool ShouldUpdatePosition() => false;

	public void DrawOverTiles(SpriteBatch spriteBatch)
	{
		const float bloomTime = 10f;

		var shatter = ParticleHandler.GetTexture(ParticleHandler.TypeOf<Shatter>());
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float opacity = (float)Projectile.timeLeft / TimeLeftMax * 0.4f;
		float bloomOp = (float)((Projectile.timeLeft - (TimeLeftMax - bloomTime)) / bloomTime);

		var color = Color.Black * opacity;
		var bloomCol = Color.Cyan.Additive() * bloomOp;

		spriteBatch.Draw(shatter, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, shatter.Size() / 2, Projectile.scale, default, 0);
		spriteBatch.Draw(shatter, Projectile.Center - Main.screenPosition, null, Color.Lerp(color * 1.5f, Color.Cyan.Additive(), bloomOp), Projectile.rotation, shatter.Size() / 2, Projectile.scale * 0.9f, default, 0);

		spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, bloomCol, 0, bloom.Size() / 2, Projectile.scale * 0.8f, default, 0);
	}
}

public class EnergyPlunge : ModProjectile
{
	public const float FallSpeed = 30f;

	public override string Texture => AssetLoader.EmptyTexture;
	public ref float StartHeight => ref Projectile.ai[0];

	public static bool CanStomp(Player player) => player.WearingSet<GraniteBody>() && !(player.mount.Active && player.mount.CanFly()) && !player.pulley;
	public static bool Stomping(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<EnergyPlunge>()] > 0;
	public static void Begin(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
			Projectile.NewProjectile(player.GetSource_FromThis("DoubleTap"), player.Center, Vector2.Zero, ModContent.ProjectileType<EnergyPlunge>(), 10, 10, player.whoAmI);

		SoundEngine.PlaySound(SoundID.DD2_LightningBugHurt with { Volume = 0.5f, Pitch = 1f, PitchVariance = 0.2f }, player.Center);
		SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { PitchVariance = 0.2f }, player.Center);

		player.velocity.Y -= 15 * player.gravDir; //Hop
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(20);
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.DamageType = DamageClass.Melee;
	}

	public override void AI()
	{
		bool justSpawned = Projectile.timeLeft > 2;
		StartHeight = justSpawned ? Projectile.Center.Y : Math.Min(Projectile.Center.Y, StartHeight);

		var owner = Main.player[Projectile.owner];
		Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter);
		Projectile.rotation = owner.velocity.ToRotation();
		Projectile.timeLeft = 2;

		if (!owner.active || owner.dead || !CanStomp(owner))
		{
			Projectile.active = false;
			return;
		}

		float odds = Math.Clamp(1f - Math.Abs(owner.velocity.Y / FallSpeed), 0.1f, 1f);
		if (Main.rand.NextFloat(odds) < 0.05f)
		{
			float mag = Main.rand.NextFloat();
			var linePos = Projectile.Center + Main.rand.NextVector2Unit() * mag * 15f;
			var lineVel = Vector2.Normalize(owner.velocity) * mag * 4f;

			ParticleHandler.SpawnParticle(new ImpactLine(linePos, lineVel, Color.Blue.Additive() * 0.5f, new Vector2(0.3f, mag * 2.5f), (int)(mag * 15)));
			ParticleHandler.SpawnParticle(new ImpactLine(linePos, lineVel, Color.White.Additive() * 0.5f, new Vector2(0.3f, mag * 2.5f) * 0.5f, (int)(mag * 15)));

			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, Scale: 0.3f);
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
		}

		if (owner.velocity.Y == 0) //Impact
			Projectile.Kill();
	}

	public override void OnKill(int timeLeft)
	{
		var owner = Main.player[Projectile.owner];

		float strength = Math.Max((Projectile.Center.Y - StartHeight) / 200f, 0);
		int damage = (int)(owner.GetDamage(DamageClass.Melee).ApplyTo(50) * strength);
		float knockback = owner.GetKnockback(DamageClass.Melee).ApplyTo(5) * strength;

		float strengthCapped = Math.Min(strength / 1.5f, 1);
		var center = Projectile.Center + new Vector2(0, owner.height / 2);

		if (!Main.dedServ)
		{
			if (Projectile.owner == Main.myPlayer)
			{
				var modifier = new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, Vector2.UnitY, Math.Max(strengthCapped, 0.5f) * 12f, 3, 20);
				Main.instance.CameraModifiers.Add(modifier);

				Projectile.NewProjectile(Projectile.GetSource_Death(), center, Vector2.Zero, ModContent.ProjectileType<EnergyImpact>(), damage, knockback, Projectile.owner, strengthCapped);
			}

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(center + new Vector2(0, 5), Color.Cyan.Additive() * strengthCapped * 0.2f, 1f,
				220, 20, "Scorch", new Vector2(2), Common.Easing.EaseFunction.EaseCircularOut));

			for (int i = 0; i < 2; i++)
			{
				var lineCol = ((i == 0) ? Color.Cyan : Color.White).Additive();
				var scale = new Vector2(1, 3) * ((i == 0) ? 1.4f : 1f) * strengthCapped;

				ParticleHandler.SpawnParticle(new ImpactLine(center + new Vector2(0, 2), Vector2.Zero, lineCol, scale, 15));
			}

			for (int i = 0; i < 20; i++)
			{
				float reach = strengthCapped * 50;
				Dust.NewDustPerfect(center + new Vector2(reach * Main.rand.NextFloat(-1f, 1f), 0), DustID.Vortex, Vector2.UnitY * -Main.rand.NextFloat(3), Scale: Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;
			}

			SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 1f - strengthCapped - 0.5f, PitchVariance = 0.2f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact with { Pitch = 0.2f, PitchVariance = 0.2f }, Projectile.Center);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var owner = Main.player[Projectile.owner];
		var effect = AssetLoader.LoadedShaders["StarjinxNoise"];
		float rotation = Projectile.rotation + ((owner.gravDir < 0f) ? MathHelper.Pi : 0);

		float opacity = Projectile.Opacity * Math.Min(owner.velocity.Y / FallSpeed, 1) * 0.8f;
		var color = new Color(30, 130, 200) * opacity;
		var center = Projectile.Center + new Vector2(-30, 0).RotatedBy(Projectile.rotation) - Main.screenPosition;

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
		
		var colorMod = new Color(20, 72, 230, Projectile.alpha).ToVector4() * opacity;
		effect.Parameters["colorMod"].SetValue(colorMod);
		effect.Parameters["noise"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
		effect.Parameters["opacity2"].SetValue(0.25f * opacity);
		effect.Parameters["counter"].SetValue(0);
		effect.CurrentTechnique.Passes[2].Apply();

		var texture = AssetLoader.LoadedTextures["Extra_49"].Value;
		Main.spriteBatch.Draw(texture, center, null, color, rotation, texture.Size() / 2, Projectile.scale * new Vector2(1.4f, 0.25f), default, 0);
		
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);

		return false;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		=> overPlayers.Add(index);

	public override bool ShouldUpdatePosition() => false;
}