using Terraria.Audio;

namespace SpiritReforged.Content.Savanna.Items.Termite;

public class TermiteJar : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 28;
		Item.value = 250;
		Item.maxStack = Item.CommonMaxStack;
		Item.useTime = 10;
		Item.useAnimation = 15;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.createTile = ModContent.TileType<TermiteJar_Tile>();
		Item.placeStyle = 0;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
	}
	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe();
		recipe.AddIngredient(Mod.Find<ModItem>("TermiteItem").Type, 5);
		recipe.AddIngredient(ItemID.Bottle, 1);
		recipe.AddTile(TileID.WorkBenches);
		recipe.Register();
	}
}
public class TermiteJar_Tile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		Main.tileFrameImportant[Type] = Main.tileFrameImportant[TileID.Bottles];
		Main.tileLavaDeath[Type] = Main.tileLavaDeath[TileID.Bottles];
		Main.tileSolidTop[Type] = Main.tileSolidTop[TileID.Bottles];
		Main.tileTable[Type] = Main.tileTable[TileID.Bottles];
		TileObjectData.addTile(Type);

		DustType = DustID.Glass;
		AnimationFrameHeight = 18;

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(200, 200, 200), name);
	}
	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		frameCounter++;
		if (frameCounter >= 8)
		{
			frameCounter = 0;
			frame++;
			frame %= 3;
		}
	}
	public sealed override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer)
		{
			if (Main.rand.NextBool(750))
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Termite/TermiteEscape") with { Volume = .8f, PitchVariance = 0.4f }, new(i * 16, j * 16));
		}
	}
}