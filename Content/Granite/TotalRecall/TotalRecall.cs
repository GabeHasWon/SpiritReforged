using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Evil.Items.Ammo;
using SpiritReforged.Content.Forest.Ammo;

namespace SpiritReforged.Content.Granite.TotalRecall;

public class TotalRecall() : ShotgunItem(new(shotMultiplier: 0.33f, spreadMultiplier: -0.2f))
{
	class TotalRecallShellParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, ShotgunAmmoItem item = null) : ShotgunShellParticle(position, velocity, scale, maxTime, item)
	{
		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);

			if (Progress < 0.33f)
			{
				float progress = Progress / 0.33f;

				Position += Main.rand.NextVector2CircularEdge(1, 1) * (1 - progress); // shake

				Velocity.Y += 0.05f;
				if (Velocity.Y > 0)
					Velocity.Y *= 1.04f;

				if (Velocity.Y > 20f)
					Velocity.Y = 20f;

				Velocity *= 0.97f;
				Velocity.X *= 0.96f;

				Rotation += Velocity.Length() * 0.02f;
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
		{
			if (Progress < 0.33f)
			{
				var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

				float progress = 1f - Progress / 0.33f;

				spriteBatch.Draw(bloom, Position - Main.screenPosition, null, Color.LightCyan.Additive() * progress * 0.15f, 0f, bloom.Size() / 2f, Scale * 0.2f, 0f, 0f);
			}

			base.Draw(ref settings, spriteBatch);
		}
	}

	int _shootDirection;
	float _shootRotation;

	public const int MAX_COOLDOWN = 120;
	int cooldown;
	public override bool AltFunctionUse(Player player) => cooldown <= 0 && Main.projectile.Any(p => p.active && p.owner == player.whoAmI && p.TryGetGlobalProjectile<TotalRecallGlobalProjectile>(out var gp) && gp._active && gp._lingerTime > 0);
	public override void SafeSetDefaults()
	{
		Item.damage = 24;
		Item.knockBack = 8f;

		Item.width = 66;
		Item.height = 26;
		
		Item.useTime = Item.useAnimation = 32;

		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;

		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Orange;
		Item.autoReuse = true;
	}

	public override void UpdateInventory(Player player)
	{
		if (cooldown > 0)
			cooldown--;
	}

	public override void UseItemFrame(Player player)
	{
		ItemVisualHelpers.SetGunUseItemFrame(player, _shootDirection, _shootRotation, -0.25f, EaseFunction.EaseQuinticOut, EaseFunction.EaseOutBack(2.2f), false, new(0.05f, 0.95f));
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		ItemVisualHelpers.SetGunUseStyle(player, Item, _shootDirection, -12, EaseFunction.EaseQuinticOut, EaseFunction.EaseOutBack(2.2f), new Vector2(66, 26), new Vector2(-16, -4), new(0.05f, 0.95f));
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_shootRotation = (player.Center - Main.MouseWorld).ToRotation();
		_shootDirection = Main.MouseWorld.X < player.Center.X ? -1 : 1;

		if (player.altFunctionUse == 2)
		{
			TotalRecallGlobalProjectile.Pullback(player);

			cooldown = MAX_COOLDOWN;

			return false;
		}

		return base.Shoot(player, source, position, velocity, type, damage, knockback);
	}

	public override void AdditionalShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, ShotgunAmmoItem ammo, int damage, float knockback)
	{
		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());
		Vector2 shellPosition = position + new Vector2(12, -10 * player.direction).RotatedBy(velocity.ToRotation());

		ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPosition, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 4f) - Vector2.UnitY * Main.rand.NextFloat(4f), 1f, 120, ammo));
	}
}

public class TotalRecallGlobalProjectile : GlobalProjectile
{
	public const int MAX_LINGER_TIME = 500;
	public const int MAX_LIFE_TIME = 30;

	// all bullets that count as slugs
	public readonly static List<int> slugIDs = [ModContent.ProjectileType<SlugProjectile>()];

	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.CountsAsClass<RangedDamageClass>();

	public Vector2 _pullbackPosition;
	public Vector2 _originalVelocity;

	public bool _pullingBack;
	public bool _active;
	public bool _red;

	public int _maxPullbackTime;
	public int _lingerTime;
	public int _lifeTime;

	public float _bulletRotation;

	public TotalRecallDistortionHandler.DistortionShaderItem _shaderItem;

	public bool IsSlug(Projectile projectile) => slugIDs.Contains(projectile.type);
	public float LifetimeProgress(Projectile projectile) => _lifeTime / (float)(MAX_LIFE_TIME * (IsSlug(projectile) ? 2f : 1f) * projectile.extraUpdates);

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse_WithAmmo ammoSource && ammoSource.Item.ModItem is not null and TotalRecall totalRecall)
		{
			_red = Main.rand.NextBool();
			_active = true;
			_maxPullbackTime = projectile.timeLeft;
			_bulletRotation = Main.rand.NextFloat(6.28f);
			_originalVelocity = projectile.velocity;
		}
	}

	public override bool? CanDamage(Projectile projectile)
	{
		if (_active && !_pullingBack)
		{
			return projectile.timeLeft > 1;
		}

		return base.CanDamage(projectile);
	}

	public override bool PreAI(Projectile projectile)
	{
		if (_active && !_pullingBack)
		{
			if (projectile.timeLeft > 2)
			{
				_lifeTime++;

				projectile.timeLeft = (int)MathHelper.Lerp(_maxPullbackTime, 2, LifetimeProgress(projectile));
				projectile.velocity *= 0.995f;
				if (IsSlug(projectile))
					projectile.velocity *= 0.99f;
			}
			else
			{
				_bulletRotation += Main.rand.NextFloat(-0.05f, 0.05f) + projectile.velocity.Length() * 0.2f;

				projectile.timeLeft = 2;

				if (projectile.velocity.LengthSquared() > 0)
					projectile.velocity *= 0.96f;

				projectile.velocity.Y += 0.005f;

				if (_shaderItem is null)
				{
					_shaderItem = new();
					_shaderItem.timeActive = 2;
					TotalRecallDistortionHandler.AddItem(_shaderItem);
				}
				else
				{
					float lingerProgress = _lingerTime / (float)(MAX_LINGER_TIME * projectile.extraUpdates);

					_shaderItem.Position = projectile.Center;
					_shaderItem.timeActive = 2;
					_shaderItem.Scale = 0.75f * (1 - lingerProgress) + projectile.scale * (1 - lingerProgress) * 1.66f;

					if (IsSlug(projectile))
						_shaderItem.Scale *= 1.2f;

					_shaderItem.DistortionStrength = MathHelper.Lerp(1.5f, 0.2f, 1 - EaseBuilder.EaseQuinticOut.Ease(lingerProgress));
					_shaderItem.ColorInterpolant = _red ? 1 - lingerProgress : lingerProgress;
				}

				if (++_lingerTime > MAX_LINGER_TIME * projectile.extraUpdates)
				{
					projectile.Kill();
					ParticleRenderers.UnderProjectiles.Add(new TotalRecallBulletParticle(projectile.Center, -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(3f), 1f, 120, IsSlug(projectile)));
				}

				return false;
			}	
		}

		return true;
	}

	public override void AI(Projectile projectile)
	{
		if (_active && _pullingBack)
		{
			if (projectile.Distance(_pullbackPosition) < 50f)
				projectile.Kill();
			else
			{
				// little bit of homing
				projectile.velocity += projectile.DirectionTo(_pullbackPosition) * 1f;
			}
		}
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		var bulletTex = ModContent.Request<Texture2D>("SpiritReforged/Content/Granite/TotalRecall/TotalRecallSmallBullet").Value;
		var star = AssetLoader.LoadedTextures["Star"].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		if (IsSlug(projectile))
			bulletTex = ModContent.Request<Texture2D>("SpiritReforged/Content/Granite/TotalRecall/TotalRecallSlugBullet").Value;

		if (!_pullingBack)
		{
			float lingerProgress = _lingerTime / (float)(MAX_LINGER_TIME * projectile.extraUpdates);

			float fade = 1f;

			if (lingerProgress < 0.2f)
				fade = lingerProgress / 0.2f;

			float outlineFade = fade;

			if (lingerProgress > 0.8f)
				outlineFade = 1f - (lingerProgress - 0.8f) / 0.2f;
	
			Color color = Color.Lerp(Color.LightCyan, Color.MediumVioletRed, _red ? 1 - lingerProgress : lingerProgress);

			Vector2 drawPosition = projectile.Center + Main.rand.NextVector2Circular(2f, 2f) * lingerProgress - Main.screenPosition;

			Main.spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, color.Additive() * outlineFade * 0.15f, 0f, bloom.Size() / 2f, projectile.scale * 0.2f, 0f, 0f);

			DrawHelpers.DrawOutline((offset) => Main.spriteBatch.Draw(bulletTex, drawPosition + offset, null, color.Additive() * outlineFade, _bulletRotation, bulletTex.Size() / 2f, 1f, 0f, 0f));

			Main.spriteBatch.Draw(bulletTex, drawPosition, null, Color.White * fade, _bulletRotation, bulletTex.Size() / 2f, 1f, 0f, 0f);
		}

		return _lingerTime <= 0 || _pullingBack;
	}

	public static void Pullback(Player player)
	{
		bool activated = false;

		foreach (Projectile p in Main.ActiveProjectiles)
		{
			if (p.active && p.TryGetGlobalProjectile<TotalRecallGlobalProjectile>(out var gp) && gp._active && gp._lingerTime > 0)
			{
				if (gp._shaderItem is not null)
					ParticleRenderers.OverSolids.Add(new TotalRecallDistortionParticle(p.Center, gp._shaderItem.Scale, gp._shaderItem.DistortionStrength, gp._shaderItem.ColorInterpolant, 25, gp.IsSlug(p)));

				p.damage *= 2;
				p.velocity = p.DirectionTo(player.Center) * gp._originalVelocity.Length() * 2f;
				p.timeLeft = gp._maxPullbackTime;
				gp._pullingBack = true;
				gp._pullbackPosition = player.Center;

				activated = true;
			}
		}

		if (activated)
		{
			if (player.whoAmI == Main.myPlayer)
				ScreenshakeHelper.Shake(player.Center, Main.rand.NextVector2CircularEdge(2f,2f), 0.66f, 2, 10);

			ParticleRenderers.OverSolids.Add(new TotalRecallDistortionParticle(player.Center, 2, 1, Main.rand.NextFloat(), 35));
		}
	}
}

class TotalRecallBulletParticle(Vector2 position, Vector2 velocity, float scale, int maxTime, bool slug = false) : ShotgunShellParticle(position, velocity, scale, maxTime, null)
{
	public override string TexturePath => AssetLoader.EmptyTexture;

	internal bool Slug = slug;
	public static readonly Asset<Texture2D> SmallTex = Graphics.Tex("SpiritReforged/Content/Granite/TotalRecall/TotalRecallSmallBullet");
	public static readonly Asset<Texture2D> SlugTex = Graphics.Tex("SpiritReforged/Content/Granite/TotalRecall/TotalRecallSlugBullet");

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
	{
		Texture2D texture = Slug ? SlugTex.Value : SmallTex.Value;

		spriteBatch.Draw(texture, Position - Main.screenPosition, null, Color.White, Rotation, texture.Size() / 2f, Scale, 0f, 0f);
	}
}

class TotalRecallDistortionParticle : Particle
{
	public override string TexturePath => AssetLoader.EmptyTexture;

	public TotalRecallDistortionHandler.DistortionShaderItem _shaderItem;

	internal float _distortionStrength;
	internal float _colorInterpolant;
	internal bool _slug;

	public TotalRecallDistortionParticle(Vector2 position, float scale, float distortionStrength, float colorInterpolant, int maxTime, bool slug = false)
	{
		Position = position;
		Scale = scale;
		_distortionStrength = distortionStrength;
		_colorInterpolant = colorInterpolant;
		MaxTime = maxTime;
		_slug = slug;

		_shaderItem = new();
		_shaderItem.timeActive = 2;
		TotalRecallDistortionHandler.AddItem(_shaderItem);
	}

	public override void Update(ref ParticleRendererSettings settings)
	{
		base.Update(ref settings);

		if (_shaderItem is not null)
		{
			_shaderItem.Position = Position;
			_shaderItem.timeActive = 2;
			_shaderItem.Scale = MathHelper.Lerp(Scale, Scale * 2.2f, EaseBuilder.EaseQuinticOut.Ease(Progress));

			if (_slug)
				_shaderItem.Scale *= 1.2f;

			_shaderItem.DistortionStrength = MathHelper.Lerp(_distortionStrength, 0f, EaseBuilder.EaseCubicOut.Ease(Progress));
			_shaderItem.ColorInterpolant = _colorInterpolant;
		}
	}
}

