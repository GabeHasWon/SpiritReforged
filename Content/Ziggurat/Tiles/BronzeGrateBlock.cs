using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class BronzeGrateBlock : ModTile, IAutoloadTileItem
{
	public static readonly SoundStyle Damage = new("SpiritReforged/Assets/SFX/Tile/MetalBreak")
	{
		PitchVariance = 0.5f
	};

	public void AddItemRecipes(ModItem item) => item.CreateRecipe(2).AddIngredient(AutoContent.ItemType<BronzePlating>()).AddTile(TileID.WorkBenches).Register();

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = false;

		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;
		SpiritSets.AllowsLiquid[Type] = true;

		this.Merge(ModContent.TileType<BronzePlating>());
		AddMapEntry(new Color(179, 146, 107));

		DustType = DustID.Copper;
		HitSound = Damage;
	}
}