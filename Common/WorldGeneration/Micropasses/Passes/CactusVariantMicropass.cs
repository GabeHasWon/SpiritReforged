using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles.CactiVariants;
using Terraria.IO;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

/// <summary>
/// Handles generating and populating cactus variants for each desert. This is not synced, as it's all server side.
/// </summary>
internal class CactusVariantMicropass : Micropass
{
	public override string WorldGenName => "Cactus Variants";

	/// <summary>
	/// Stores the cactus IDs for each desert in <see cref="DesertMapping.DesertInstances"/>. Stores a string since this is saved/loaded.
	/// </summary>
	[WorldBound]
	public static Dictionary<int, string> CactusTypePerDesertById = [];

	public static int GetVariantForDesert(int id) => ModContent.Find<ModTile>(CactusTypePerDesertById[id]).Type;
	public static int GetVariantForDesert(DesertMapping.DesertInstance instance) => ModContent.Find<ModTile>(CactusTypePerDesertById[instance.Index]).Type;

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Cactus, Palm Trees, & Coral");

	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		foreach (var instance in DesertMapping.DesertInstances)
		{
			int tries = 0;
			ModTile tile = ModContent.GetInstance<BunnyEarCacti>();
			CactusTypePerDesertById.Add(instance.Key, tile.FullName);

			int y = instance.Value.Bounds.Top;
			int count = WorldGen.genRand.Next(2, Math.Max(3, instance.Value.Bounds.Right - instance.Value.Bounds.Left / 40));

			for (int i = 0; i < 2; ++i)
			{
				int x = WorldGen.genRand.Next(instance.Value.Bounds.Left, instance.Value.Bounds.Right);

				if (tries > 20000)
					break;

				if (WorldUtils.Find(new Point(x, y), new Searches.Down(200).Conditions(new Conditions.IsSolid()), out Point floor))
				{
					var check = Placer.PlaceTile(floor.X, floor.Y - 1, tile.Type);

					if (check.success || tries > 20000)
					{
						var data = TileObjectData.GetTileData(tile.Type, 0);

						if (data.HookPostPlaceMyPlayer.hook is not null)
						{
							Point pos = new(floor.X, floor.Y - 1);
							TileExtensions.GetTopLeft(ref pos.X, ref pos.X);
							data.HookPostPlaceMyPlayer.hook.Invoke(pos.X, pos.Y, tile.Type, 0, 0, 0);
						}

						continue;
					}
					else
						tries++;
				}
			}
		}
	}
}

public class CactusVariantSaving : ModSystem
{
	public override void SaveWorldData(TagCompound tag)
	{
		if (CactusVariantMicropass.CactusTypePerDesertById.Count > 0)
		{
			int count = 0;
			tag.Add("count", CactusVariantMicropass.CactusTypePerDesertById.Count);

			foreach (var pair in CactusVariantMicropass.CactusTypePerDesertById)
			{
				TagCompound instance = [];
				instance.Add("key", pair.Key);
				instance.Add("value", pair.Value);

				tag.Add("instance" + count, instance);
				count++;
			}
		}
	}

	public override void LoadWorldData(TagCompound tag)
	{
		CactusVariantMicropass.CactusTypePerDesertById.Clear();

		if (tag.TryGet("count", out int count))
		{
			for (int i = 0; i < count; i++)
			{
				TagCompound t = tag.GetCompound("instance" + i);
				int key = t.GetInt("key");
				string value = t.GetString("value");
				CactusVariantMicropass.CactusTypePerDesertById.Add(key, value);
			}
		}
	}
}