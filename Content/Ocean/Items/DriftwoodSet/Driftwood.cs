using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

[FromClassic("Driftwood1Item")]
public class SmallDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace("SmallDriftwoodItem", "Driftwood");

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Type, new Terraria.DataStructures.DrawAnimationVertical(2, 3) { NotActuallyAnimating = true, Frame = 0 });

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SmallDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D tex = TextureAssets.Item[Type].Value;
		var frame = Main.itemAnimations[Type].GetFrame(tex);

		spriteBatch.Draw(tex, Item.position - Main.screenPosition, frame, GetAlpha(lightColor) ?? lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 10).Register();
}

public class SmallDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}

[FromClassic("Driftwood2Item")]
public class MediumDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace("MediumDriftwoodItem", "Driftwood");

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Type, new Terraria.DataStructures.DrawAnimationVertical(2, 3) { NotActuallyAnimating = true, Frame = 1 });

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<MediumDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D tex = TextureAssets.Item[Type].Value;
		var frame = Main.itemAnimations[Type].GetFrame(tex);

		spriteBatch.Draw(tex, Item.position - Main.screenPosition, frame, GetAlpha(lightColor) ?? lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 20).Register();
}

public class MediumDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}

[FromClassic("Driftwood3Item")]
public class LargeDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace("LargeDriftwoodItem", "Driftwood");

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Type, new Terraria.DataStructures.DrawAnimationVertical(2, 3) { NotActuallyAnimating = true, Frame = 2 });

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<LargeDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D tex = TextureAssets.Item[Type].Value;
		var frame = Main.itemAnimations[Type].GetFrame(tex);

		spriteBatch.Draw(tex, Item.position - Main.screenPosition, frame, GetAlpha(lightColor) ?? lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
		return false;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 25).Register();
}

public class LargeDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}