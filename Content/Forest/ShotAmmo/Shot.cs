using Mono.Cecil;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Terraria;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.ShotAmmo;
public class Shot : ShotgunAmmoItem
{
	static void Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		for (int i = 0; i < shotCount; i++)
		{
			Vector2 spreadDir = direction;

			if (spreadAmount > 0f && i != 0) // no spread on first shot
				spreadDir = direction.RotatedByRandom(spreadAmount);

			Projectile.NewProjectile(source, position, spreadDir * speed * Main.rand.NextFloat(0.5f, 1.5f), ProjectileID.Bullet, damage, knockback, player.whoAmI);
		}
	}

	public Shot() : base(Behavior, 6, .4f, 10f) { }
}
