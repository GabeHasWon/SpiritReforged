using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> A naturally-generating amber fossil with no associated item. </summary>
public class AmberFossil : ShiningAmber
{
	public override string Texture => DrawHelpers.RequestLocal(GetType(), nameof(PolishedAmber));

	public static int GetContainedItem(int i, int j)
	{
		int id = ModContent.GetInstance<FossilEntity>().Find(i, j);
		return (id == -1) ? 0 : (TileEntity.ByID[id] as FossilEntity).itemType;
	}

	private static int SelectItem()
	{
		var result = new WeightedRandom<int>();

		result.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Dragonflies));
		result.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Fireflies));
		result.AddRange(1f, Recipes.GetTypesFromGroup(RecipeGroupID.Butterflies));
		result.AddRange(1f, ItemID.Grasshopper, ItemID.Frog);
		result.AddRange(0.05f, ItemID.GoldFrog, ItemID.GoldDragonfly, ItemID.GoldGrasshopper);

		return result;
	}

	private static void PlaceEntity(int i, int j)
	{
		if (ModContent.GetInstance<FossilEntity>().Find(i, j) != -1)
			return; //An entity already exists here

		int itemType = SelectItem();
		int id = ModContent.GetInstance<FossilEntity>().Place(i, j);

		((FossilEntity)TileEntity.ByID[id]).itemType = itemType;

		if (Main.netMode != NetmodeID.SinglePlayer)
			new FossilData((short)i, (short)j, itemType).Send();
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		RegisterItemDrop(AutoContent.ItemType<PolishedAmber>());
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (resetFrame)
			PlaceEntity(i, j);

		return base.TileFrame(i, j, ref resetFrame, ref noBreak);
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !fail)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), GetContainedItem(i, j));

			ModContent.GetInstance<FossilEntity>().Kill(i, j);
		}
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		int type = GetContainedItem(i, j);
		Main.instance.LoadItem(type);

		var texture = TextureAssets.Item[type].Value;
		var position = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition;
		float rotation = (i + j) * 0.25f % MathHelper.TwoPi;

		spriteBatch.Draw(texture, position, null, Lighting.GetColor(i, j) * 0.4f, rotation, texture.Size() / 2, 1, default, 0);
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileDrawing.IsVisible(Main.tile[i, j]))
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomSolid);

		return base.PreDraw(i, j, spriteBatch);
	}
}

public class FossilEntity : ModTileEntity
{
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