using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Underground.Pottery;

/// <summary> Specialised pot item template for <see cref="PotteryWheel"/>. </summary>
/// <param name="baseName"> The internal name of the tile this item places. </param>
public sealed class AutoloadedPotItem(string baseName, StyleDatabase.StyleGroup group, AutoloadedPotItem.RecipeDelegate recipe = null) : ModItem
{
	public delegate void RecipeDelegate(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition);

	protected override bool CloneNewInstances => true;
	public override string Name => _group.name + "Item";
	public override string Texture => DrawHelpers.RequestLocal(GetType(), _group.name);

	private string _baseName = baseName;
	private StyleDatabase.StyleGroup _group = group;

	private ModTile Tile => Mod.Find<ModTile>(_baseName);

	public override ModItem Clone(Item newEntity)
	{
		var item = base.Clone(newEntity) as AutoloadedPotItem;
		item._baseName = _baseName;
		item._group = _group;
		return item;
	}

	public override void SetStaticDefaults() //Register echo tile drops for all styles
	{
		List<int> styles = [];
		int styleLimit = _group.styles.Length;

		for (int i = 0; i < styleLimit; i++)
			styles.Add(_group.styles[i]);
		
		if (styles.Count != 0)
			Tile.RegisterItemDrop(Type, [.. styles]);
	}

	public override void SetDefaults() => Item.DefaultToPlaceableTile(Tile.Type, _group.styles[0]);
	public override void AddRecipes() => recipe?.Invoke(this, _group, new(Discovered, RecordedPot));

	/// <summary> Localized text "Discovered". Normally used in tandem with <see cref="RecordedPot"/> to create a condition. </summary>
	public static LocalizedText Discovered => Language.GetText("Mods.SpiritReforged.Conditions.Discovered");
	/// <summary> Whether <see cref="_group"/> is discovered by the local player. </summary>
	public bool RecordedPot() => Main.LocalPlayer.GetModPlayer<RecordPlayer>().IsValidated(_group.name);
}