namespace SpiritReforged.Content.Ziggurat.BeetleMinion;

public class ScarabStaff : ModItem
{
	public sealed class DungBeetlePlayer : ModPlayer
	{
		/// <summary> Whether the local player has struck an NPC with a whip. </summary>
		public bool struckTarget;

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (ProjectileID.Sets.IsAWhip[proj.type])
				struckTarget = true;
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 40;
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Green;
		Item.mana = 10;
		Item.damage = 12;
		Item.knockBack = 2.5f;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = Item.useAnimation = 30;
		Item.DamageType = DamageClass.Summon;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<DungBeetleMinion>();
		Item.UseSound = SoundID.Item44;
		Item.autoReuse = true;
	}
}