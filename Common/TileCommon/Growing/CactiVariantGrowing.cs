using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

namespace SpiritReforged.Common.TileCommon.Growing;

/// <summary>
/// Handles growing cactus variants.
/// </summary>
internal class CactiVariantGrowing : GlobalTile
{
	public override void RandomUpdate(int i, int j, int type)
	{
		if (Main.tileSand[Main.tile[i, j].TileType] && DesertMapping.TryGetDesert(i, j, out var desert) && WorldGen.genRand.NextBool(1200))
		{
			int cactus = CactusVariantMicropass.GetVariantForDesert(desert);
			var check = Placer.PlaceTile(i, j - 1, cactus).Send();

			if (check.success)
			{
				var data = TileObjectData.GetTileData(cactus, 0);

				if (data.HookPostPlaceMyPlayer.hook is not null)
				{
					Point pos = new(i, j - 1);
					TileExtensions.GetTopLeft(ref pos.X, ref pos.X);
					data.HookPostPlaceMyPlayer.hook.Invoke(pos.X, pos.Y, cactus, 0, 0, 0);
				}
			}
		}
	}
}
