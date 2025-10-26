using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Stand;
using SpiritReforged.Content.Jungle.Misc;
using Terraria.DataStructures;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Discoveries.Passes;

internal class BoStaffDiscovery : Discovery
{
	public override string WorldGenName => "Bo Staff";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, List<Discovery> discoveries, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Jungle Chests"));
	}

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Discoveries");

		int type = ModContent.TileType<SwordStand>();
		for (int a = 0; a < GenVars.numJChests; a++)
		{
			Point origin = new(GenVars.JChestX[a], GenVars.JChestY[a]);

			WorldGen.PlaceTile(origin.X, origin.Y, type, true, style: 2);
			if (Framing.GetTileSafely(origin).TileType == type)
			{
				TileExtensions.GetTopLeft(ref origin.X, ref origin.Y);
				int id = ModContent.GetInstance<SwordStand.SwordStandSlot>().Place(origin.X, origin.Y);

				if (id != -1 && TileEntity.ByID[id] is SwordStand.SwordStandSlot slot)
					slot.item = new(ModContent.ItemType<BoStaff>());

				break;
			}
		}
	}
}
