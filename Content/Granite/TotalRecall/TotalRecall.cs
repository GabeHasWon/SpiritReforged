using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using System.Linq;
using Terraria;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Granite.TotalRecall;

public class TotalRecall() : ShotgunItem(new(shotMultiplier: 0.33f, spreadMultiplier: -0.2f))
{
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

	}
}

public class TotalRecallGlobalProjectile : GlobalProjectile
{
	public const int MAX_LINGER_TIME = 300;
	public const int MAX_LIFE_TIME = 30;
	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.CountsAsClass<RangedDamageClass>();

	public Vector2 _pullbackPosition;

	public int _maxPullbackTime;
	public bool _pullingBack;

	public int _lingerTime;
	public int _lifeTime;
	public bool _active;

	public DistortionHandler.DistortionShaderItem _shaderItem;

	public float LifetimeProgress(Projectile projectile) => _lifeTime / (float)(MAX_LIFE_TIME * projectile.extraUpdates);

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse_WithAmmo ammoSource && ammoSource.Item.ModItem is not null and TotalRecall totalRecall)
		{
			_active = true;
			_maxPullbackTime = projectile.timeLeft;
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
			}
			else
			{
				if (_shaderItem is null)
				{
					_shaderItem = new();
					DistortionHandler.AddItem(_shaderItem);
				}
				else
				{
					float lingerProgress = _lingerTime / (float)(MAX_LINGER_TIME * projectile.extraUpdates);

					_shaderItem.Position = projectile.Center;
					_shaderItem.timeActive = 2;
					_shaderItem.Progress = lingerProgress;
					_shaderItem.Scale = projectile.scale * (1 - lingerProgress);
					_shaderItem.tintColor = Color.White;
				}

				projectile.timeLeft = 2;

				if (projectile.velocity.LengthSquared() > 0)
					projectile.velocity *= 0.9f;

				if (++_lingerTime > MAX_LINGER_TIME * projectile.extraUpdates)
					projectile.Kill();

				return false;
			}	
		}

		return true;
	}

	public override void AI(Projectile projectile)
	{
		if (_active)
		{
			if (_pullingBack)
			{
				projectile.velocity *= 1.02f;

				return;
			}
		}
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		var bulletTex = ModContent.Request<Texture2D>("SpiritReforged/Content/Granite/TotalRecall/TotalRecallBullet").Value;
		var star = AssetLoader.LoadedTextures["Star"].Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = LifetimeProgress(projectile);

		if (_pullingBack)
		{
			progress = projectile.timeLeft / (float)_maxPullbackTime;

			Main.spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, Color.CornflowerBlue.Additive() * progress * 0.2f, 0f, bloom.Size() / 2f, 0.3f * projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(bulletTex, projectile.Center - Main.screenPosition, null, Color.White * progress, 0f, bulletTex.Size() / 2f, 2f * projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.CadetBlue.Additive() * progress * 0.4f, 0f, star.Size() / 2f, 0.3f * projectile.scale, 0f, 0f);
		}
		else if (progress > 0.5f)
		{
			float opacity = (progress - 0.5f) / 0.5f;

			Main.spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, Color.CornflowerBlue.Additive() * opacity * 0.2f, 0f, bloom.Size() / 2f, 0.3f * projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(bulletTex, projectile.Center - Main.screenPosition, null, Color.White * opacity, 0f, bulletTex.Size() / 2f, 2f * projectile.scale, 0f, 0f);
			Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.CadetBlue.Additive() * opacity * 0.4f, 0f, star.Size() / 2f, 0.3f * projectile.scale, 0f, 0f);
		}

		return _lingerTime <= 0 || _pullingBack;
	}

	public static void Pullback(Player player)
	{
		foreach (Projectile p in Main.ActiveProjectiles)
		{
			if (p.active && p.TryGetGlobalProjectile<TotalRecallGlobalProjectile>(out var gp) && gp._active && gp._lingerTime > 0)
			{
				p.damage *= 2;
				p.velocity = p.DirectionTo(player.Center) * 16f;
				p.timeLeft = gp._maxPullbackTime;
				gp._pullingBack = true;
				gp._pullbackPosition = player.Center;
			}
		}
	}
}
