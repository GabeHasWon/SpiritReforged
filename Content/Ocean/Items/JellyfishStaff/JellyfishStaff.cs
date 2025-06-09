using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ocean.Items.JellyfishStaff;

[AutoloadGlowmask("255, 255, 255")]
public class JellyfishStaff : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.PinkJellyfish), ItemDropRule.Common(Type, 100)));
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.BlueJellyfish), ItemDropRule.Common(Type, 500)));

		MoRHelper.AddElement(Item, MoRHelper.Thunder);
		MoRHelper.AddElement(Item, MoRHelper.Water, true);
	}

	public override void SetDefaults()
	{
		Item.width = 52;
		Item.height = 46;
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.mana = 10;
		Item.damage = 12;
		Item.knockBack = 2.5f;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.DamageType = DamageClass.Summon;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<JellyfishMinion>();
		Item.UseSound = SoundID.Item44;
		Item.autoReuse = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => position = Main.MouseWorld;
	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => player.altFunctionUse != 2;
	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, .224f * 2, .133f * 2, .255f * 2);
}