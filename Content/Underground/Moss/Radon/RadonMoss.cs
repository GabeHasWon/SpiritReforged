using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.Moss.Radon;

[AutoloadGlowmask("224,232,70")]
public class RadonMoss : OganessonMoss
{
	public override void SetEntry()
	{
		RegisterItemDrop(ModContent.ItemType<RadonMossItem>());

		AddMapEntry(new Color(252, 248, 3), this.GetLocalization("MapEntry"));
		AddMapEntry(new Color(252, 248, 3), LocalizedText.Empty); // Register two map entries & only use 1 in GetMapOption for Recipe Browser functionality w/o in-game changes

		DustType = ModContent.DustType<RadonMossDust>();
		HitSound = SoundID.Grass;
	}

	public override ushort GetMapOption(int i, int j) => 1;

	public override void RandomUpdate(int i, int j)
	{
		SpreadHelper.Spread(i, j, Type, 1, DirtType); //Try spread moss
		SpreadHelper.Spread(i, j, ModContent.TileType<RadonMossGrayBrick>(), 1, TileID.GrayBrick); //Also spread to gray bricks

		GrowPlants(i, j);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		yield return new Item(ItemID.StoneBlock);
	}

	public override void GrowPlants(int i, int j) => Placer.PlacePlant<RadonPlants>(i, j, Main.rand.Next(RadonPlants.StyleRange));
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.234f, 0.153f, 0.03f);
}