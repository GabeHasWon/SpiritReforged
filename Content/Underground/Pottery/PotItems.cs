namespace SpiritReforged.Content.Underground.Pottery;

/// <summary> Specialised pot item autoloader (<see cref="AutoloadedPotItem"/>) for <see cref="PotteryWheel"/>. </summary>
public class PotItems : ILoadable
{
	//Both sort by index
	private static readonly string[] UncommonNames = ["Cavern", "Gold", "Ice", "Desert", "Jungle", "Dungeon", "Corrupt", "Crimson", "Marble", "Hell"];
	private static readonly string[] CommonNames = ["Cavern", "Ice", "Jungle", "Dungeon", "Hell", "Corrupt", "Spider", "Crimson", "Pyramid", "Temple", "Marble", "Desert"];

	public void Load(Mod mod)
	{
		for (int i = 0; i < UncommonNames.Length; i++)
		{
			string name = UncommonNames[i];
			mod.AddContent(new AutoloadedPotItem(nameof(BiomePotsEcho), name, i * 3));
		}

		for (int i = 0; i < CommonNames.Length; i++)
		{
			string name = CommonNames[i];
			int style = i * 9;

			if (style != 0)
				style += 3; //Account for cavern pot odd style count

			mod.AddContent(new AutoloadedPotItem(nameof(CommonPotsEcho), "Ancient" + name, style, (i == 0) ? 12 : 9));
		}
	}

	public void Unload() { }
}

/// <param name="baseName"> The internal name of the tile this item places. </param>
/// <param name="name"></param>
/// <param name="style"> The base style this item places implicitly affected by RandomStyleRange. </param>
/// <param name="styleLimit"> The length of styles this item can place, used for calculating drops. </param>
public sealed class AutoloadedPotItem(string baseName, string name, int style, int styleLimit = 3) : ModItem
{
	protected override bool CloneNewInstances => true;
	public override string Name => _name + "PotItem";
	public override string Texture => (GetType().Namespace + $".{_name}Pot").Replace('.', '/');

	private readonly int _styleLimit = styleLimit;

	private string _baseName = baseName;
	private string _name = name;
	private int _style = style;

	private ModTile Tile => Mod.Find<ModTile>(_baseName);

	public override ModItem Clone(Item newEntity)
	{
		var item = base.Clone(newEntity) as AutoloadedPotItem;
		item._baseName = _baseName;
		item._name = _name;
		item._style = _style;
		return item;
	}

	public override void SetStaticDefaults() //Register echo tile drops for all styles
	{
		List<int> styles = [];

		for (int i = 0; i < _styleLimit; i++)
			styles.Add(_style + i);
		
		if (styles.Count != 0)
			Tile.RegisterItemDrop(Type, [.. styles]);
	}

	public override void SetDefaults() => Item.DefaultToPlaceableTile(Tile.Type, _style);
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup("ClayAndMud", 5)
		.AddTile(ModContent.TileType<PotteryWheel>()).Register();
}