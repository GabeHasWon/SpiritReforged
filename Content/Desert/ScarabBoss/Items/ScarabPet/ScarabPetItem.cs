using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.ScarabPet;

[AutoloadGlowmask("255,255,255")]
internal class ScarabPetItem : ModItem
{
	public class ScarabPetBuff : PetBuff<ScarabPetProjectile>
	{
		protected override (string, string) BuffInfo => ("Tiny Scarab", "'It really loves to roll...'");
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<ScarabPetProjectile>();
		Item.buffType = ModContent.BuffType<ScarabPetBuff>();
		Item.UseSound = SoundID.NPCDeath6; 
		Item.rare = ItemRarityID.Master;
		Item.master = true;
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}
