using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.Moss.Radon;

public class RadonMossItem : OganessonMossItem
{
	public override int GetPlaceType(int tileType) => tileType switch
	{
		TileID.GrayBrick => ModContent.TileType<RadonMossGrayBrick>(),
		TileID.Stone => ModContent.TileType<RadonMoss>(),
		_ => -1
	};

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, 0.252f, 0.228f, 0.03f);
}