using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Silk;

[AutoloadEquip(EquipType.Legs)]
public class SilkSirwal : EquippableItem
{
	public static int AltEquipSlot { get; private set; }
	public override void Load()
	{
		if (!Main.dedServ)
		{
			const string name = "SilkSirwalAlt_Legs";
			AltEquipSlot = EquipLoader.AddEquipTexture(Mod, DrawHelpers.RequestLocal(GetType(), name), EquipType.Legs, null, name);
		}

		PlayerEvents.OnPostUpdateRunSpeeds += static (p) =>
		{
			if (p.HasEquip<SilkSirwal>() && p.velocity.Y == 0) //Only apply while grounded
				p.runAcceleration += 0.2f;
		};
	}

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });
	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 18;
		Item.value = 10000;
		Item.rare = ItemRarityID.Blue;
		Item.defense = 3;
	}

	public override void UpdateEquippable(Player player)
	{
		if (player.velocity.Y == 0) //Only apply while grounded
			player.moveSpeed += 0.1f;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		frame.Y = (frame.Height + 2) * (Main.LocalPlayer.Male ? 0 : 1);
		spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, Item.GetAlpha(drawColor), 0, origin, scale, default, 0);
		return false;
	}

	public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
	{
		if (!male && !Main.dedServ)
			equipSlot = AltEquipSlot;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.Silk, 10)
		.AddRecipeGroup("GoldBars", 2)
		.AddTile(TileID.Anvils)
		.Register();
}