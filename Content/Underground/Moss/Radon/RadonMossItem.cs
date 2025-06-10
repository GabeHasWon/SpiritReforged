using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.Moss.Radon;

public class RadonMossItem : OganessonMossItem
{
	public override bool? UseItem(Player player)
	{
		var tile = player.TargetTile();
		if (tile.TileType == TileID.Stone)
		{
			TryPlace(ModContent.TileType<RadonMoss>());
			return true;
		}
		else if (tile.TileType == TileID.GrayBrick)
		{
			TryPlace(ModContent.TileType<RadonMossGrayBrick>());
			return true;
		}

		return null;

		static void TryPlace(int type)
		{
			WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, type, forced: true);
			var t = Main.tile[Player.tileTargetX, Player.tileTargetY];

			if (t.TileType == type && Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY);
		}
	}

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, .252f, .228f, .03f);
}