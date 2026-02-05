using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.TileCommon.Tree;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Misc;

public class WateringCanTwo : ModItem
{
	public class WateringCanTwoHeld : WateringCan.WateringCanHeld
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = NumStyles;

		public override void OnWaterLocation(Vector2 worldPosition)
		{
			base.OnWaterLocation(worldPosition);

			Point tilePosition = worldPosition.ToTileCoordinates();

			if ((Main.player[Projectile.owner].DoBootsEffect_PlaceFlowersOnTile(tilePosition.X, tilePosition.Y) || FertilizerGlobalProjectile.GrowTree(tilePosition.X, tilePosition.Y)) && !Main.dedServ)
				TileSwayHelper.SetWindTime(tilePosition.X, tilePosition.Y, Vector2.UnitX);
		}
	}

	public enum Style { SpeedGrow, Living }

	public static int NumStyles => Enum.GetNames<Style>().Length;
	protected override bool CloneNewInstances => true;

	public Style style;

	public override void SetStaticDefaults() => VariantGlobalItem.AddVariants(Type, NumStyles, false);

	public override void SetDefaults()
	{
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(silver: 80);
		Item.channel = true;
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<WateringCanTwoHeld>();

		style = (Style)Main.rand.Next(NumStyles);
		SetVisualStyle();
	}

	public override ModItem Clone(Item itemClone)
	{
		var myClone = (WateringCanTwo)base.Clone(itemClone);
		myClone.style = style;
		return myClone;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai1: (byte)style);
		return false;
	}

	public override void SaveData(TagCompound tag) => tag[nameof(style)] = (byte)style;
	public override void LoadData(TagCompound tag)
	{
		style = (Style)tag.Get<byte>(nameof(style));
		SetVisualStyle();
	}

	public override void NetSend(BinaryWriter writer) => writer.Write((byte)style);
	public override void NetReceive(BinaryReader reader)
	{
		style = (Style)reader.ReadByte();
		SetVisualStyle();
	}

	private void SetVisualStyle()
	{
		if (!Main.dedServ && Item.TryGetGlobalItem(out VariantGlobalItem v))
			v.subID = (byte)style;
	}
}