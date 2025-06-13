using System.Linq;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Automatically generates an item that places the given <see cref="ModTile"/> down.<br/>
/// The <see cref="StaticItemDefaults"/>, <see cref="SetItemDefaults"/> and <see cref="AddItemRecipes"/> hooks can be used to conveniently modify the generated item. </summary>
public interface IAutoloadTileItem
{
	// These are already defined on ModTiles and shortens the autoloading code a bit.
	public string Name { get; }
	public string Texture { get; }

	public void StaticItemDefaults(ModItem item) { }
	public void SetItemDefaults(ModItem item) { }
	public void AddItemRecipes(ModItem item) { }
}

public class AutoloadTileItemHandler : ILoadable
{
	public const string Item = "Item";

	public void Load(Mod mod)
	{
		var valid = typeof(ModTile);
		var types = AssemblyManager.GetLoadableTypes(mod.Code).Where(x => typeof(IAutoloadTileItem).IsAssignableFrom(x) && !x.IsAbstract);

		foreach (var item in types)
		{
			if (!valid.IsAssignableFrom(item))
				throw new InvalidCastException(nameof(IAutoloadTileItem) + $" should be placed on only {valid.Name}s!");

			var instance = Activator.CreateInstance(item) as IAutoloadTileItem;
			mod.AddContent(new AutoloadedTileItem(instance.Name + Item, instance.Texture + Item, instance));
		}
	}
	public void Unload() { }
}

/// <summary> Represents an item autoloaded with <see cref="IAutoloadTileItem"/>. </summary>
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
		Item.DefaultToPlaceableTile(Mod.Find<ModTile>(_internalName.Replace(AutoloadTileItemHandler.Item, string.Empty)).Type);
		_hooks.SetItemDefaults(this);
	}

	public override void AddRecipes() => _hooks.AddItemRecipes(this);
}