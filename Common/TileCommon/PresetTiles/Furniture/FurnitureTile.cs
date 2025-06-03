using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class FurnitureTile : ModTile, IAutoloadTileItem
{
	public ModItem ModItem => this.AutoModItem();

	/// <summary> The defining material in most furniture recipes. </summary>
	public virtual int CoreMaterial => ItemID.None;

	public virtual void StaticItemDefaults(ModItem item) { }
	public virtual void SetItemDefaults(ModItem item) { }
	public virtual void AddItemRecipes(ModItem item) { }

	public sealed override void SetStaticDefaults()
	{
		if (ModItem.Type > 0)
			RegisterItemDrop(ModItem.Type);

		StaticDefaults();
	}

	/// <inheritdoc cref="ModBlockType.SetStaticDefaults"/>
	public virtual void StaticDefaults() { }
}
