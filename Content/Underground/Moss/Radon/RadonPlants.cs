using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.Moss.Radon;

public class RadonPlants : OganessonPlants
{
	public override void SetStaticDefaults()
	{
		TileDefaults();
		ObjectData(new Color(206, 209, 23), ModContent.TileType<RadonMoss>(), ModContent.TileType<RadonMossGrayBrick>());
		
		DustType = ModContent.DustType<RadonMossDust>();
		HitSound = SoundID.Grass;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		int type = Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(), 16, 16)].HeldItem.type;
		int drop = ModContent.ItemType<RadonMossItem>();

		if (type == ModContent.ItemType<LandscapingShears>())
		{
			if (Main.rand.NextBool(2))
				yield return new Item(drop);
		}
		else if (type is ItemID.PaintScraper or ItemID.SpectrePaintScraper)
		{
			if (Main.rand.NextBool(9))
				yield return new Item(drop);
		}
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = Main.rand.Next(1, 3);
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.338f, 0.219f, 0.04f);
}