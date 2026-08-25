using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items.Pepperbox;

public class Pepperbox() : ShotgunItem(new ShotgunStats())
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedEyeOfCthulhu)));

	public override void SafeSetDefaults()
	{
		Item.damage = 8;
		Item.knockBack = 6;
		Item.width = 40;
		Item.height = 20;
		Item.useTime = Item.useAnimation = 25;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.shootSpeed = 8f;

		var globalItem = Item.GetGlobalItem<MagazineGlobalItem>();

		globalItem.ActivateMagazine(new(SoundID.Item36, -0.2f, 0.3f, 4, 60), new(52, 24), new(-24, 6), true, -6, -0.15f);
		globalItem.SetAnimations(new(0.04f, 0.96f));
	}
}
