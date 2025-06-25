using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Savanna.Items.Gar;

[AutoloadBuff]
public class QuenchPotion : ModItem
{
	public static int BuffType { get; private set; }

	#region detours
	public override void Load()
	{
		BuffPlayer.QuickBuff += FocusQuenchPotion;
		BuffPlayer.ModifyBuffTime += QuenchifyBuff;
	}

	/// <summary> Forces this potion to be used before all others with quick buff. </summary>
	private static void FocusQuenchPotion(Player player)
	{
		if (!player.cursed && !player.CCed && !player.dead && !player.HasBuff(BuffType) && player.CountBuffs() < Player.MaxBuffs)
		{
			int itemIndex = player.FindItemInInventoryOrOpenVoidBag(ModContent.ItemType<QuenchPotion>(), out bool inVoidBag);

			if (itemIndex > 0)
			{
				var item = inVoidBag ? player.bank4.item[itemIndex] : player.inventory[itemIndex];

				ItemLoader.UseItem(item, player);
				player.AddBuff(item.buffType, item.buffTime);

				if (item.consumable && ItemLoader.ConsumeItem(item, player) && --item.stack <= 0)
					item.TurnToAir();
			}
		}
	}

	/// <summary> Improves buff times with <see cref="QuenchPotion_Buff"/>. </summary>
	private static void QuenchifyBuff(int buffType, ref int buffTime, Player player, bool quickBuff)
	{
		if (!Main.debuff[buffType] && buffType != BuffType && player.HasBuff(BuffType))
			buffTime = (int)(buffTime * 1.25f);
	}
	#endregion

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 20;
		BuffType = BuffAutoloader.SourceToType[GetType()];
	}

	public override void SetDefaults()
	{
		Item.width = 20;
		Item.height = 30;
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.DrinkLiquid;
		Item.useTime = Item.useAnimation = 20;
		Item.consumable = true;
		Item.autoReuse = false;
		Item.buffType = BuffType;
		Item.buffTime = 60 * 45;
		Item.value = 200;
		Item.UseSound = SoundID.Item3;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BottledWater).AddIngredient(AutoContent.ItemType<NPCs.Gar.Gar>())
		.AddIngredient(ItemID.Blinkroot).AddIngredient(ItemID.Moonglow).AddIngredient(ItemID.Waterleaf).AddTile(TileID.Bottles).Register();
}