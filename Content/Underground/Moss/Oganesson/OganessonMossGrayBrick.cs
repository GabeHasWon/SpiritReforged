using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Dusts;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

[AutoloadGlowmask("255,255,255")]
public class OganessonMossGrayBrick : GrassTile
{
	protected override int DirtType => TileID.GrayBrick;

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileLighted[Type] = true;

		RegisterItemDrop(ItemID.GrayBrick);
		AddMapEntry(new Color(220, 220, 220));
		this.Merge(TileID.Stone, TileID.GrayBrick);

		DustType = ModContent.DustType<OganessonMossDust>();
		HitSound = SoundID.Grass;
	}

	public override void RandomUpdate(int i, int j)
	{
		SpreadHelper.Spread(i, j, Type, 1, DirtType); //Try spread moss
		SpreadHelper.Spread(i, j, ModContent.TileType<OganessonMoss>(), 1, TileID.Stone); //Also spread to stone

		GrowPlants(i, j);
	}

	public override void GrowPlants(int i, int j) => Placer.PlacePlant<OganessonPlants>(i, j, Main.rand.Next(OganessonPlants.StyleRange));
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.3f, 0.3f, 0.3f);
}