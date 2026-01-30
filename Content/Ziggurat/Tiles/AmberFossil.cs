using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileMerging;
using SpiritReforged.Content.Desert.DragonFossil;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace SpiritReforged.Content.Desert.Tiles;

/// <summary> A naturally-generating amber fossil with no associated item. </summary>
public class AmberFossil : EntityTile<FossilEntity>
{
	private static WeightedRandom<int> RandomItem;

	public static int GetContainedItem(int i, int j)
	{
		int id = ModContent.GetInstance<FossilEntity>().Find(i, j);
		return (id == -1) ? 0 : (TileEntity.ByID[id] as FossilEntity).itemType;
	}

	public static FossilEntity PlaceEntity(int i, int j)
	{
		if (ModContent.GetInstance<FossilEntity>().Find(i, j) != -1)
			return null; //An entity already exists here

		int itemType = RandomItem;
		int id = ModContent.GetInstance<FossilEntity>().Place(i, j);

		var entity = (FossilEntity)TileEntity.ByID[id];
		entity.itemType = itemType;

		if (Main.netMode != NetmodeID.SinglePlayer)
			new FossilEntity.FossilData((short)i, (short)j, itemType).Send();

		return entity;
	}

	public override string Texture => ModContent.GetInstance<PolishedAmber>().Texture;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileLighted[Type] = true;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(Color.Orange);
		RegisterItemDrop(AutoContent.ItemType<PolishedAmber>());
		this.Merge(ModContent.TileType<PolishedAmber>(), ModContent.TileType<AmberFossil>(), ModContent.TileType<AmberFossilSafe>(), TileID.Sand);

		DustType = DustID.GemAmber;
		MineResist = 0.5f;

		#region loot
		RandomItem = new();

		RandomItem.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Dragonflies));
		RandomItem.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Fireflies));
		RandomItem.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Butterflies));
		RandomItem.AddRange(1f, ItemID.Grasshopper, ItemID.Frog);
		RandomItem.AddRange(0.05f, ItemID.GoldFrog, ItemID.GoldDragonfly, ItemID.GoldGrasshopper);
		RandomItem.AddRange(0.04f, ModContent.ItemType<TinyDragon>());

		if (CrossMod.Fables.CheckFind("StormlionLarvaItem", out ModItem stormlionLarva))
			RandomItem.Add(stormlionLarva.Type, 0.08f);

		if (CrossMod.Thorium.CheckFind("SpikedBracer", out ModItem bracer))
			RandomItem.Add(bracer.Type, 0.04f);

		if (CrossMod.Verdant.CheckFind("SnailShellBlockItem", out ModItem snailShell))
			RandomItem.Add(snailShell.Type, 0.005f);

		#endregion
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (resetFrame)
			PlaceEntity(i, j);

		return TileFraming.Gemspark(i, j, resetFrame);
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !fail)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), GetContainedItem(i, j));

			LocalEntity.Kill(i, j);
		}
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		int type = GetContainedItem(i, j);
		Main.instance.LoadItem(type);

		var texture = TextureAssets.Item[type].Value;
		var position = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition;
		float rotation = (i + j) * 0.25f % MathHelper.TwoPi;

		Color color = Lighting.GetColor(i, j);

		if (Main.LocalPlayer.findTreasure)
			color = TileExtensions.GetSpelunkerTint(color);
		else
			color *= 0.4f;

		spriteBatch.Draw(texture, position, null, color, rotation, texture.Size() / 2, 1, default, 0);
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out _))
			return false;

		Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomSolid);
		PolishedAmber.Overlay.AddToGrid(i, j);

		TileExtensions.DrawSingleTile(i, j, true, TileExtensions.TileOffset);
		TileMerger.DrawMerge(spriteBatch, i, j, color, TileExtensions.TileOffset, TileID.Sand);
		return false;
	}
}

/// <summary> A placeable amber fossil. </summary>
public class AmberFossilSafe : AmberFossil, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		StartRecipe().AddRecipeGroup(RecipeGroupID.Fireflies).Register();
		StartRecipe().AddRecipeGroup(RecipeGroupID.Dragonflies).Register();
		StartRecipe().AddRecipeGroup(RecipeGroupID.Butterflies).Register();
		StartRecipe().AddIngredient(ItemID.Grasshopper).Register();
		StartRecipe().AddIngredient(ItemID.Frog).Register();

		Recipe StartRecipe() => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		RegisterItemDrop(this.AutoItemType());
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !fail)
			ModContent.GetInstance<FossilEntity>().Kill(i, j);
	}
}

public class FossilEntity : ModTileEntity
{
	internal class FossilData : PacketData
	{
		private readonly short _x;
		private readonly short _y;
		private readonly int _itemType;

		public FossilData() { }
		public FossilData(short x, short y, int itemType)
		{
			_x = x;
			_y = y;
			_itemType = itemType;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			int itemType = reader.ReadInt32();

			if (Main.netMode == NetmodeID.Server) //Relay to other clients
				new FossilData(x, y, itemType).Send(ignoreClient: whoAmI);

			((FossilEntity)TileEntity.ByID[ModContent.GetInstance<FossilEntity>().Place(x, y)]).itemType = itemType;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_x);
			modPacket.Write(_y);
			modPacket.Write(_itemType);
		}
	}

	public int itemType;

	public override bool IsTileValidForEntity(int x, int y) => TileLoader.GetTile(Main.tile[x, y].TileType) is AmberFossil;
	public override void NetSend(BinaryWriter writer) => writer.Write(itemType);
	public override void NetReceive(BinaryReader reader) => itemType = reader.ReadInt32();

	public override void SaveData(TagCompound tag)
	{
		if (ItemLoader.GetItem(itemType) is ModItem modItem)
		{
			tag["modName"] = modItem.Mod.Name;
			tag["itemName"] = modItem.Name;
		}
		else
		{
			tag["modName"] = "Terraria";
			tag["itemType"] = itemType;
		}
	}

	public override void LoadData(TagCompound tag)
	{
		string source = tag.GetString("modName");
		int type = (source == "Terraria") ? tag.GetInt("itemType") : (Mod.TryFind(tag.GetString("itemName"), out ModItem modItem) ? modItem.Type : ItemID.None);

		itemType = type;
	}
}