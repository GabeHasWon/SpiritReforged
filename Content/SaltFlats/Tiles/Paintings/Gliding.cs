using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.SaltFlats.Tiles.Paintings;

public class Gliding : PaintingTile
{
	public override Point TileSize => new(4, 2);
	public override void StaticDefaults()
	{
		base.StaticDefaults();
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Painter, new NPCShop.Entry(Type, Condition.TimeDay, SpiritConditions.InSaltFlats)));
	}

	public override void AddItemRecipes(ModItem item) 
	{
		if (CrossMod.Thorium.Enabled && CrossMod.Thorium.TryFind("BlankPainting", out ModItem canvas))
			item.CreateRecipe().AddIngredient(canvas.Type).AddIngredient(AutoContent.ItemType<SaltBlockDull>(), 8).AddTile(TileID.WorkBenches).Register();
	}
}