using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Desert.NPCs.TownBeetle;
using Terraria.Chat;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class BeetleLicense : ModItem
{
	public const string UsedLicense = "usedBeetleLicense";

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 5;

	public override void SetDefaults()
	{
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.consumable = true;
		Item.useAnimation = 45;
		Item.useTime = 45;
		Item.UseSound = SoundID.Item92;
		Item.width = 28;
		Item.height = 28;
		Item.maxStack = Item.CommonMaxStack;
		Item.SetShopValues(ItemRarityColor.Green2, Item.buyPrice(0, 5));
	}

	public override bool? UseItem(Player player)
	{
		if (player.ItemAnimationJustStarted && (!WorldSystem.CheckWorldFlag(UsedLicense) || NPC.AnyNPCs(ModContent.NPCType<BeetleTownPet>())))
		{
			if (player.whoAmI == Main.myPlayer)
				UseLicense();

			return true;
		}

		return false;
	}

	private void UseLicense()
	{
		Color color = new(50, 255, 130);

		if (!WorldSystem.CheckWorldFlag(UsedLicense))
		{
			WorldSystem.SetWorldFlag(UsedLicense, true);
			ChatHelper.BroadcastChatMessage(NetworkText.FromKey(this.GetLocalizationKey("LicenseUse")), color);
			NetMessage.TrySendData(MessageID.WorldData);
		}
		else if (NPC.RerollVariationForNPCType(ModContent.NPCType<BeetleTownPet>()))
		{
			ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Misc.PetExchangeSuccess"), color);
		}
		else
		{
			ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Misc.PetExchangeFail"), color);
		}
	}
}