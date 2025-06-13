using System.Linq;

namespace SpiritReforged.Common.TileCommon;

/// <summary>
/// Automatically generates an item that places the given <see cref="ModTile"/> down.<br/>
/// The <see cref="SetItemDefaults(ModItem)"/> and <see cref="AddItemRecipes(ModItem)"/> hooks can be used to modify the generated item.
/// </summary>
public interface IAutoloadTileItem
{
	// These are already defined on ModTiles and shortens the autoloading code a bit.
	public string Name { get; }
	public string Texture { get; }

	public void StaticItemDefaults(ModItem item) { }
	public void SetItemDefaults(ModItem item) { }
	public void AddItemRecipes(ModItem item) { }
}

public class AutoloadTileItemSystem : ILoadable
{
	public const string Item = "Item";

	public void Load(Mod mod)
	{
		var instances = mod.GetContent<ModItem>().Where(x => x is IAutoloadTileItem);
		foreach (var item in instances)
		{
			var tileItem = item as IAutoloadTileItem;
			mod.AddContent(new AutoloadedTileItem(tileItem.Name + Item, tileItem.Texture + Item, tileItem));
		}
	}

	public void Unload() { }
}

public class AutoloadedTileItem(string name, string texture, IAutoloadTileItem hooks) : ModItem
{
	protected override bool CloneNewInstances => true;
	public override string Name => _internalName;
	public override string Texture => _texture;

	private string _internalName = name;
	private string _texture = texture;
	private IAutoloadTileItem _hooks = hooks;

	public override ModItem Clone(Item newEntity)
	{
		var item = base.Clone(newEntity) as AutoloadedTileItem;
		item._internalName = _internalName;
		item._texture = _texture;
		item._hooks = _hooks;
		return item;
	}

	public override void SetStaticDefaults() => _hooks.StaticItemDefaults(this);

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(Mod.Find<ModTile>(_internalName.Replace(AutoloadTileItemSystem.Item, string.Empty)).Type);
		_hooks.SetItemDefaults(this);
	}

	public override void AddRecipes() => _hooks.AddItemRecipes(this);
}