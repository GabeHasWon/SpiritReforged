using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;

namespace SpiritReforged.Common.EmoteCommon;

public abstract class CustomEmote : ModEmoteBubble
{
	public virtual int Category => EmoteID.Category.General;

	public override void SetStaticDefaults() => AddToCategory(Category);

	public override bool PreDraw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, SpriteEffects spriteEffects)
	{
		Texture2D bubbleTexture = TextureAssets.Extra[ExtrasID.EmoteBubble].Value;
		Rectangle bubbleFrame = bubbleTexture.Frame(8, 39, EmoteBubble.IsFullyDisplayed ? 1 : 0);

		spriteBatch.Draw(bubbleTexture, position, bubbleFrame, Color.White, 0f, origin, 1f, spriteEffects, 0f);

		if (!EmoteBubble.IsFullyDisplayed)
			return false;

		spriteBatch.Draw(texture, position, frame, Color.White, 0f, origin, 1f, spriteEffects, 0f);
		return false;
	}

	public override bool PreDrawInEmoteMenu(SpriteBatch spriteBatch, EmoteButton uiEmoteButton, Vector2 position, Rectangle frame, Vector2 origin)
	{
		Color borderColor = uiEmoteButton.Hovered ? Main.OurFavoriteColor : Color.Black;
		Rectangle bubbleFrame = uiEmoteButton.BubbleTexture.Frame(8, 39, 1, 0);

		spriteBatch.Draw(uiEmoteButton.BubbleTexture.Value, position, bubbleFrame, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
		spriteBatch.Draw(uiEmoteButton.EmoteTexture.Value, position, frame, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
		spriteBatch.Draw(uiEmoteButton.BorderTexture.Value, position - Vector2.One * 2f, null, borderColor, 0f, origin, 1f, SpriteEffects.None, 0f);

		return false;
	}
}