using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Cloud.Items;
using System.Linq;

namespace SpiritReforged.Content.Forest.Cloud;

/// <summary> Handles Cloudstalk growth under cloud blocks. </summary>
internal class CloudGlobalTile : GlobalTile
{
	public override void RandomUpdate(int i, int j, int type)
	{
		float chance = type switch
		{
			TileID.RainCloud => .001f,
			TileID.SnowCloud => .0001f,
			TileID.Cloud => 0.005f,
			_ => 0
		};

		if (CrossMod.Remnants.Enabled)
			chance *= 0.05f; // Much, much rarer in the cloud-heavy Remnants

		if (WorldGen.SolidOrSlopedTile(i, j + 1) || Main.rand.NextFloat() >= chance)
			return;

		while (!WorldGen.SolidOrSlopedTile(i, j + 1))
			j++;

		var ground = Main.tile[i, j + 1];
		var tile = Main.tile[i, j];

		int herbType = ModContent.TileType<CloudstalkTile>();
		bool validAnchor = TileObjectData.GetTileData(herbType, 0).AnchorValidTiles.Contains(ground.TileType);

		if (validAnchor && ground.Slope == SlopeType.Solid && !ground.IsHalfBlock && tile.LiquidAmount < 100 && Placer.IsReplaceable(i, j) && Placer.CanPlaceHerb(i, j, herbType))
		{
			tile.ClearTile();
			Placer.PlaceTile(i, j, herbType).Send();
		}
	}
}