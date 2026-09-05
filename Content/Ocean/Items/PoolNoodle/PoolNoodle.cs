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

	public const int NUM_STYLES = 3;

	public byte Style
	{
		get => _style;
		set
		{
			_style = value;

			if (!Main.dedServ && Main.ContentLoaded && Item.TryGetGlobalItem(out VariantItemRenderer global))
				global.subID = value;
		}
	}
	private byte _style;

	public override string Texture => base.Texture + "0";

	public override void SetStaticDefaults()
	{
		VariantItemRenderer.VariantCounts[Type] = 3;

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

		Style = (byte)Main.rand.Next(NUM_STYLES);
	}

	public override ModItem Clone(Item itemClone)
	{
		var myClone = (PoolNoodle)base.Clone(itemClone);
		myClone.Style = Style;
		return myClone;
	}

	public override bool MeleePrefix() => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai1: Style);
		return false;
	}

	public override void SaveData(TagCompound tag) => tag[nameof(Style)] = Style;
	public override void LoadData(TagCompound tag) => Style = tag.Get<byte>(nameof(Style));

	public override void NetSend(BinaryWriter writer) => writer.Write(Style);
	public override void NetReceive(BinaryReader reader) => Style = reader.ReadByte();
}