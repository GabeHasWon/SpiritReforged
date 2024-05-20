namespace SpiritReforged.Content.Ocean.Items;

public class DeepCascadeShard : ModItem
{
	private int subID = -1; //Controls the in-world sprite for this item

	public override void SetDefaults()
	{
		Item.width = 10;
		Item.height = 18;
		Item.value = 400;
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		if (subID == -1)
			subID = Main.rand.Next(3);

		Lighting.AddLight(new Vector2(Item.Center.X, Item.Center.Y), 207 * 0.001f, 12 * 0.001f, 12 * 0.001f);

		Texture2D tex = ModContent.Request<Texture2D>(Texture + "_World").Value;
		var frame = new Rectangle(0, 18 * subID, 10, 16);
		int num7 = 16;
		float num8 = (float)(Math.Cos(Main.GlobalTimeWrappedHourly % 2.4 / 2.4 * MathHelper.TwoPi) / 5 + 0.5);
		var color2 = new Color(252, 57, 3, 100);
		spriteBatch.Draw(tex, Item.Center - Main.screenPosition, frame, lightColor, rotation, new Vector2(Item.width, Item.height) / 2, scale, SpriteEffects.None, 0f);

		for (int index2 = 0; index2 < num7; ++index2)
		{
			Color color3 = Item.GetAlpha(color2) * (0.85f - num8);
			Vector2 position2 = Item.Center + ((index2 / num7 * MathHelper.TwoPi) + rotation).ToRotationVector2() * (4.0f * num8 + 2.0f) - Main.screenPosition - new Vector2(tex.Width, tex.Height) * Item.scale / 2f + new Vector2(Item.width, Item.height) / 2 * Item.scale;
			spriteBatch.Draw(tex, position2 + new Vector2(-4, 20), frame, color3, rotation, new Vector2(Item.width, Item.height) / 2, Item.scale * 1.05f, SpriteEffects.None, 0.0f);
		}

		return false;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		subID = -1;
		return true;
	}

	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
	{
		Texture2D tex2 = ModContent.Request<Texture2D>(Texture + "_World_Glow", AssetRequestMode.ImmediateLoad).Value;
		var frame = new Rectangle(0, 18 * subID, 10, 16);

		spriteBatch.Draw(tex2, Item.Center - Main.screenPosition, frame, Color.White, rotation, new Vector2(Item.width, Item.height) / 2, scale, SpriteEffects.None, 0f);
	}
}
