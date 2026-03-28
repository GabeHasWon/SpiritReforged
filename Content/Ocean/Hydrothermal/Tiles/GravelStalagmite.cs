using RubbleAutoloader;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

public class GravelStalagmite : ModTile, IAutoloadRubble
{
	public IAutoloadRubble.RubbleData Data => new(AutoContent.ItemType<Gravel>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(90, 90, 90));
		DustType = DustID.Asphalt;
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		/*if (!visible || tile.TileFrameY != 0)
			return;

		Vector2 position = new Vector2(i, j).ToWorldCoordinates();
		if (Main.rand.NextBool(5)) //Passive smoke effects
		{
			var velocity = new Vector2(0, -Main.rand.NextFloat(2f, 2.5f));
			ParticleHandler.SpawnParticle(new SmokeCloud(position, velocity, new Color(40, 40, 50), Main.rand.NextFloat(0.1f, 0.15f), EaseFunction.EaseQuadOut, Main.rand.Next(50, 120), false)
			{
				SecondaryColor = Color.SlateGray,
				TertiaryColor = Color.Black,
				ColorLerpExponent = 0.5f,
				Intensity = 0.25f,
				Pixellate = true,
				PixelDivisor = 4
			});
		}

		if (Main.rand.NextBool(12))
			ParticleHandler.SpawnParticle(new BubbleParticle(position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(4), -Vector2.UnitY, Main.rand.NextFloat(0.2f, 0.35f), 60));

		if (Main.rand.NextBool()) //Passive ash effects
		{
			float range = Main.rand.NextFloat();
			Vector2 velocity = new Vector2(0, -Main.rand.NextFloat(range * 8f)).RotatedByRandom((1f - range) * 1.5f);
			Dust.NewDustPerfect(position, DustID.Ash, velocity, Alpha: 180).noGravity = true;
		}*/ //REMOVE
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
}