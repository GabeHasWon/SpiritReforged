using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Ocean.Items.PoolNoodle;

public class PoolNoodle : ModItem
{
	protected override bool CloneNewInstances => true;

	public const int NumStyles = 3;
	public byte style = NumStyles;

	public override void SetStaticDefaults()
	{
		VariantGlobalItem.AddVariants(Type, NumStyles, false);

		ItemLootDatabase.AddItemRule(ItemID.OceanCrate, ItemDropRule.Common(Type, 8));
		ItemLootDatabase.AddItemRule(ItemID.OceanCrateHard, ItemDropRule.Common(Type, 8));

		MoRHelper.AddElement(Item, MoRHelper.Water, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToWhip(ModContent.ProjectileType<PoolNoodleProj>(), 14, 0, 4);
		Item.width = Item.height = 38;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 45);

		style = (byte)Main.rand.Next(NumStyles);
	}

	public override ModItem Clone(Item itemClone)
	{
		var myClone = (PoolNoodle)base.Clone(itemClone);
		myClone.style = style;
		return myClone;
	}

	public override bool MeleePrefix() => true;
	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai1: style);
		return false;
	}

	public override void SaveData(TagCompound tag) => tag[nameof(style)] = style;
	public override void LoadData(TagCompound tag)
	{
		style = tag.Get<byte>(nameof(style));
		SetVisualStyle();
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(style);
	public override void NetReceive(BinaryReader reader)
	{
		style = reader.ReadByte();
		SetVisualStyle();
	}

	private void SetVisualStyle()
	{
		if (!Main.dedServ && Item.TryGetGlobalItem(out VariantGlobalItem v))
			v.subID = style;
	}
}