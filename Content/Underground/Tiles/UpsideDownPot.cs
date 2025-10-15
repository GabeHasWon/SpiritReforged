using RubbleAutoloader;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using Terraria.DataStructures;
using static SpiritReforged.Common.TileCommon.NamedStyles;
using static SpiritReforged.Common.WorldGeneration.WorldMethods;

namespace SpiritReforged.Content.Underground.Tiles;

public class UpsideDownPot : PotTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0] } };

	public override TileRecord AddRecord(int type, StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		return record.AddRating(5).AddDescription(Language.GetText(TileRecord.DescKey + ".UpsideDown"));
	}

	public override void AddObjectData()
	{
		const int row = 1;

		Main.tileOreFinderPriority[Type] = 575;
		Main.tileCut[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = -1;
	}

	public override void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("Mods.SpiritReforged.Items.UpsideDownPotItem.DisplayName"));

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Generating || Autoloader.IsRubble(Type))
			return;

		var source = new EntitySource_TileBreak(i, j);
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			Item.NewItem(source, new Rectangle(i * 16, j * 16, 32, 32), ModContent.ItemType<PotHead>());
		}
	}
}

[AutoloadEquip(EquipType.Head)]
public class PotHead : ModItem
{
	public static int EquipSlot { get; private set; }

	public override void Load()
	{
		PlayerEvents.OnKill += DropPot;
		On_Player.KillMe += HideGores;
	}

	private static void DropPot(Player p)
	{
		if (Main.myPlayer == p.whoAmI && p.head == EquipSlot)
		{
			var velocity = new Vector2(0, -Main.rand.NextFloat(2, 5)).RotatedByRandom(1);
			Projectile.NewProjectile(p.GetSource_Death(), p.Top, velocity, ModContent.ProjectileType<FallingPot>(), 0, 0, p.whoAmI);
		}
	}

	private static void HideGores(On_Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
	{
		orig(self, damageSource, dmg, hitDirection, pvp);

		if (self.head == EquipSlot)
			self.immuneAlpha = 255;
	}

	public override void SetStaticDefaults()
	{
		EquipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);

		if (CrossMod.Fables.Enabled)
		{
			if (CrossMod.Fables.TryFind("PeculiarPot", out ModItem peculiarPot))
			{
				ItemID.Sets.ShimmerTransformToItem[Type] = peculiarPot.Type;
				ItemID.Sets.ShimmerTransformToItem[peculiarPot.Type] = Type;
			}
		}
	}
	public override void SetDefaults()
	{
		Item.Size = new(24);
		Item.value = Item.sellPrice(gold: 3);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}