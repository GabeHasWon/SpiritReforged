using SpiritReforged.Common.Subclasses.Shotguns;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Granite.TotalRecall;

public class TotalRecall() : ShotgunItem(new(shotMultiplier: 0.33f, spreadMultiplier: -0.2f))
{
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

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
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
	public const int MAX_PULLBACK_TIME = 20;
	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.CountsAsClass<RangedDamageClass>();

	public Vector2 _pullbackPosition;
	public int _pullbackTime;
	public int _lingerTime;
	public bool _active;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse_WithAmmo ammoSource && ammoSource.Item.ModItem is not null and TotalRecall totalRecall)
		{
			_active = true;
			_lingerTime = MAX_LINGER_TIME;
		}
	}

	public override void AI(Projectile projectile)
	{
		if (_active)
		{
			if (_pullbackTime > 0)
			{
				projectile.position = Vector2.Lerp(projectile.position, _pullbackPosition, 1 - _pullbackTime / (float)MAX_PULLBACK_TIME);
				projectile.timeLeft = 2;

				_pullbackTime--;
				if (_pullbackTime == 0)
					projectile.Kill();

				return;
			}

			if (projectile.timeLeft < 2 && _lingerTime > 0)
			{
				projectile.timeLeft = 2;
				projectile.velocity *= 0.9f;
				_lingerTime--;
			}
		}
	}

	public static void Pullback(Player player)
	{
		foreach (Projectile p in Main.ActiveProjectiles)
		{
			if (p.active && p.TryGetGlobalProjectile<TotalRecallGlobalProjectile>(out var gp) && gp._active && gp._lingerTime > 0)
			{
				p.timeLeft = 2;
				gp._pullbackTime = MAX_PULLBACK_TIME;
				gp._pullbackPosition = player.Center;
			}
		}
	}
}
