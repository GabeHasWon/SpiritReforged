using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.SimpleEntity;

namespace SpiritReforged.Content.Ocean.Items.Buoys;

[FromClassic("BigBuoyItem")]
public class BigBuoy : SmallBuoy
{
	public override bool? UseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted)
		{
			SimpleEntitySystem.NewEntity<BigBuoyEntity>(Main.MouseWorld);
			return true;
		}

		return null;
	}

	public override void AddRecipes() => CreateRecipe().AddRecipeGroup("CopperBars", 3).AddIngredient(ItemID.Glass, 2).AddTile(TileID.Anvils).Register();
}

public class BigBuoyEntity : SmallBuoyEntity
{
	private static Asset<Texture2D> GlowTexture;

	public override Texture2D Glowmask => GlowTexture.Value;
	protected override int ItemType => ModContent.ItemType<BigBuoy>();

	public override void Load()
	{
		if (!Main.dedServ)
			GlowTexture = ModContent.Request<Texture2D>(TexturePath + "_Glow");

		saveMe = true;
		width = 46;
		height = 120;
	}
}