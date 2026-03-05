using SpiritReforged.Content.Forest.MagicPowder;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Head)]
public class LeatherHood : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AncientMarksmanHood>();

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 12;
		Item.value = Item.sellPrice(silver: 30);
		Item.rare = ItemRarityID.Blue;
		Item.defense = 1;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs)
		=> (head.type, body.type, legs.type) == (Type, ModContent.ItemType<LeatherPlate>(), ModContent.ItemType<LeatherLegs>());
	public override void UpdateEquip(Player player) => player.GetCritChance(DamageClass.Generic) += 5;

	public override void UpdateArmorSet(Player player)
	{
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Marksman");
		player.GetModPlayer<MarksmanPlayer>().active = true;

		if (player.GetModPlayer<MarksmanPlayer>().Concentrated)
			Yoraiz0rEye(player);
	}

	public override void ArmorSetShadows(Player player)
	{
		if (player.GetModPlayer<MarksmanPlayer>().Concentrated)
			player.armorEffectDrawOutlinesForbidden = true;
	}

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ItemID.Leather, 6).AddRecipeGroup(RecipeGroupID.IronBar, 2).AddTile(TileID.Anvils).Register();
		Recipe.Create(ModContent.ItemType<LeatherPlate>()).AddIngredient(ItemID.Leather, 8).AddRecipeGroup(RecipeGroupID.IronBar, 4).AddTile(TileID.Anvils).Register();
		Recipe.Create(ModContent.ItemType<LeatherLegs>()).AddIngredient(ItemID.Leather, 7).AddRecipeGroup(RecipeGroupID.IronBar, 2).AddTile(TileID.Anvils).Register();
	}

	/// <summary>
	/// The following is code adapted from vanilla from the Eye of Yoraiz0r. Did my best to celanup + comment
	///  I don't see it ever being used elsewhere. If it does, though, let me know and I will move it to its own file.
	/// The code spawns a trail of dust between a given start point and endpoint.
	/// Considerations are made for when the player sprite is mounted, on a minecart, or rotated.
	/// </summary>
	private static void Yoraiz0rEye(Player player)
	{
		int index = 0 + player.bodyFrame.Y / 56;
		if (index >= Main.OffsetsPlayerHeadgear.Length)
			index = 0;

		// Adjustments: Player in minecart
		Vector2 cartOffset = Vector2.Zero;
		if (player.mount.Active && player.mount.Cart)
		{
			int sign = Math.Sign(player.velocity.X);
			if (sign == 0)
				sign = player.direction;

			cartOffset = new Vector2(MathHelper.Lerp(0.0f, -8f, player.fullRotation / MathHelper.PiOver4), MathHelper.Lerp(0.0f, 2f, Math.Abs(player.fullRotation / 0.7853982f))).RotatedBy(player.fullRotation, new Vector2());
			if (sign == Math.Sign(player.fullRotation))
				cartOffset *= MathHelper.Lerp(1f, 0.6f, Math.Abs(player.fullRotation / MathHelper.PiOver4));
		}

		// Adjustments: Player sprite is rotated
		Vector2 spinningpoint1 = new Vector2(3 * player.direction - (player.direction == 1 ? 1 : 0), -11.5f * player.gravDir) + Vector2.UnitY * player.gfxOffY + player.Size / 2f + Main.OffsetsPlayerHeadgear[index];
		Vector2 spinningpoint2 = new Vector2(3 * player.shadowDirection[1] - (player.direction == 1 ? 1 : 0), -11.5f * player.gravDir) + player.Size / 2f + Main.OffsetsPlayerHeadgear[index];

		if (player.fullRotation != 0.0)
		{
			spinningpoint1 = spinningpoint1.RotatedBy(player.fullRotation, player.fullRotationOrigin);
			spinningpoint2 = spinningpoint2.RotatedBy(player.fullRotation, player.fullRotationOrigin);
		}

		// Adjustments: Player is mounted
		float mountOffset = 0.0f;
		if (player.mount.Active)
			mountOffset = player.mount.PlayerOffset;

		// start + endpoints
		Vector2 start = player.position + spinningpoint1 + cartOffset;
		start.Y -= mountOffset / 2f;

		Vector2 end = player.oldPosition + spinningpoint2 + cartOffset;
		end.Y -= mountOffset / 2f;

		// trail length
		int totalDist = (int)Vector2.Distance(start, end) / 5 + 1;
		if (Vector2.Distance(start, end) % 5.0 != 0.0)
			++totalDist;

		// make trail
		for (float dist = 1f; dist <= totalDist; ++dist)
		{
			Dust dust = Main.dust[Dust.NewDust(player.Center, 0, 0, DustID.GoldCoin, 0.0f, 0.0f, 0, new Color(), .8f)];
			dust.position = Vector2.Lerp(end, start, dist / totalDist);
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
			dust.customData = player;
			dust.shader = GameShaders.Armor.GetSecondaryShader(player.cYorai, player);
		}
	}
}
