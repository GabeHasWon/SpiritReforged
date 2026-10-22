using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.Crossbows;

public class HunterKeepsake : ModItem, IFlagged
{
	public class HunterModPlayer : ModPlayer
	{
		public bool critical;

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (target.life <= 0 && BoltCache.IsCrossbowBolt[proj.type])
				critical = true;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (critical && BoltCache.IsCrossbowBolt[proj.type])
			{
				critical = false;
				modifiers.SetCrit();
			}
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(32);
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}
}