using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ProjectileCommon;
using System.Linq;

namespace SpiritReforged.Content.Glyphs.Shock;

public partial class ShockGlyph
{
	public sealed class ShockPlayer : ModPlayer
	{
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && Main.myPlayer == Player.whoAmI) 
				ChannelLightning(Player, target, damageDone);			
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && proj.type != ModContent.ProjectileType<ShockGlyphLightningBolt>() && Main.myPlayer == Player.whoAmI)
				ChannelLightning(Player, target, damageDone);
		}

		public static void ChannelLightning(Player Player, NPC target, int damage)
		{
			NPC[] closestNPCs = Main.npc.Where(n => n.whoAmI != target.whoAmI && n.CanBeChasedBy(Player) && n.DistanceSQ(target.Center) < 350000f).OrderBy(n => n.DistanceSQ(target.Center)).Take(3).ToArray();

			if (closestNPCs.Length <= 0)
				return;

			for (int i = 0; i < closestNPCs.Length; i++)
			{
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero,
					ModContent.ProjectileType<ShockGlyphLightningBolt>(), (Main.hardMode ? 1 : 5) + (int)(damage * (Main.hardMode ? 0.25f : 0.35f)), 1f, Player.whoAmI, closestNPCs[i].whoAmI, ai2: i == 0 ? 1 : 0);
			}
		}

		// 10% damage increase on crits
		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				modifiers.CritDamage += 0.1f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				modifiers.CritDamage += 0.1f;
		}
	}
}