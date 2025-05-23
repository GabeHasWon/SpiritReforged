using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Ocean.Tiles;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Content.Forest.Misc;

public class TitleMusicBox : MusicBoxTile
{
	public override string MusicPath => "Assets/Music/TitleTheme";
	public override void Load() { } //Don't autoload an item
}

public class TitleMusicBoxItem : ModItem
{
	public override void SetStaticDefaults()
	{
		MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, ModContent.GetInstance<TitleMusicBox>().MusicPath), Type, ModContent.TileType<TitleMusicBox>());

		ItemID.Sets.CanGetPrefixes[Type] = false;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
	}

	public override void SetDefaults() => Item.DefaultToMusicBox(ModContent.TileType<TitleMusicBox>(), 0);
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemMethods.AutoItemType<OceanDepthsBox>()).AddIngredient(ItemMethods.AutoItemType<SavannaMusicBox>())
			.AddIngredient(ItemMethods.AutoItemType<SavannaNightMusicBox>()).AddIngredient(ItemMethods.AutoItemType<SavannaSandstormMusicBox>()).Register();
}