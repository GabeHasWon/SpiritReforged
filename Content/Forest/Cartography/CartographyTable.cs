using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Map;

namespace SpiritReforged.Content.Forest.Cartography;

public class CartographyTable : ModTile, IAutoloadTileItem
{
	[WorldBound]
	internal static WorldMap RecordedMap;

	private const int FullFrameHeight = 18 * 3;

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(191, 142, 111), this.AutoModItem().DisplayName);
		DustType = DustID.WoodFurniture;
		AnimationFrameHeight = FullFrameHeight;
	}

	public override bool RightClick(int i, int j)
	{
		SetMap();
		Main.NewText(Language.GetTextValue("Mods.SpiritReforged.Misc.UpdateMap"), new Color(255, 240, 20));

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		if (UISystem.IsActive<CatalogueUI>())
			return;

		var p = Main.LocalPlayer;

		p.cursorItemIconEnabled = true;
		p.cursorItemIconID = this.AutoItem().type;
		p.noThrow = 2;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (Main.dedServ || !TileObjectData.IsTopLeft(i, j))
			return;

		var world = new Vector2(i, j).ToWorldCoordinates(16, 16);

		if (UISystem.IsActive<CatalogueUI>() && Main.LocalPlayer.Distance(world) > 16 * 5)
			UISystem.SetInactive<CatalogueUI>();
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 4;
		}
	}

	private static void SetMap()
	{
		if (RecordedMap is null)
		{
			RecordedMap = Main.Map;
			return;
		}

		for (int x = 0; x < RecordedMap.MaxWidth; x++)
		{
			for (int y = 0; y < RecordedMap.MaxHeight; y++)
			{
				var tileToPost = Main.Map[x, y];

				if (tileToPost.Light > RecordedMap[x, y].Light)
					RecordedMap.SetTile(x, y, ref tileToPost);
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			new SyncMapData(RecordedMap).Send();
	}
}

internal class SyncMapData : PacketData
{
	private readonly WorldMap _map;

	public SyncMapData() { }
	public SyncMapData(WorldMap map) => _map = map;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		CartographyTable.RecordedMap ??= new(Main.maxTilesX, Main.maxTilesY);

		for (int x = 0; x < _map.MaxWidth; x++)
		{
			for (int y = 0; y < _map.MaxHeight; y++)
			{
				//ushort type = reader.ReadUInt16();
				byte light = reader.ReadByte(); //Only read light levels because the rest is inferred (and not written)
				//byte color = reader.ReadByte();

				var tileToPost = MapHelper.CreateMapTile(x, y, light);
				CartographyTable.RecordedMap.SetTile(x, y, ref tileToPost);
			}
		}

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new SyncMapData(CartographyTable.RecordedMap).Send(ignoreClient: whoAmI);
	}

	public override void OnSend(ModPacket modPacket)
	{
		for (int x = 0; x < _map.MaxWidth; x++)
		{
			for (int y = 0; y < _map.MaxHeight; y++)
				WriteSingleMapTile(modPacket, _map[x, y]);
		}
	}

	private static void WriteSingleMapTile(ModPacket modPacket, MapTile tile)
	{
		//modPacket.Write(tile.Type);
		modPacket.Write(tile.Light); //Only write light levels because we can infer the rest
		//modPacket.Write(tile.Color);
	}
}