using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underworld.Heirloom;
// Gives projectiles spawned from the Heirloom one pierce
public class HeirloomGlobalProjectile : GlobalProjectile
{
	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse_WithAmmo ammoSource && ammoSource.Item.ModItem is not null and Heirloom)
		{
			projectile.penetrate += 1;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = 20;
		}
	}
}
