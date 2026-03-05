using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Cartography.Maps;

public class TatteredMap : ModItem
{
	private static Point16 lastMouseWorld;

	public override void Load() => On_Player.UpdatePlacementPreview += ChangePlaceType;

	/// <summary> Shuffles the placeable tile type in the preview stage. </summary>
	/// <param name="orig"></param>
	/// <param name="self"> The local player. </param>
	/// <param name="sItem"> The item to be placed. </param>
	private static void ChangePlaceType(On_Player.orig_UpdatePlacementPreview orig, Player self, Item sItem)
	{
		if (sItem.type == ModContent.ItemType<TatteredMap>())
		{
			var point = (Main.MouseWorld / 16).ToPoint16();
			if (point != lastMouseWorld) //Shuffle the placeable type when the player hovers over a new tile on the grid
			{
				sItem.createTile = Main.rand.NextBool(3) ? ModContent.TileType<TatteredMapWallSmall>() : ModContent.TileType<TatteredMapWall>();
				lastMouseWorld = point;
			}
		}

		orig(self, sItem);
	}

	public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<TatteredMapWall>());
	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<TornMapPiece>(), 2).AddTile(TileID.WorkBenches).Register();
}