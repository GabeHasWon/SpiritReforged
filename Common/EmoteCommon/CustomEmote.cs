using SpiritReforged.Common.Visuals;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;

namespace SpiritReforged.Common.EmoteCommon;

public sealed class CustomEmote : ModEmoteBubble
{
	public override string Name => _name;
	public override string Texture => _texture;
	protected override bool CloneNewInstances => true;

	/// <summary> Whether conditions are fulfilled for this emote to appear during NPC interaction. </summary>
	public bool Active => _activeCondition.Invoke();

	private string _name;
	private string _texture;
	private int _category;
	private Func<bool> _activeCondition;

	public override ModEmoteBubble Clone(EmoteBubble newEntity)
	{
		var emote = base.Clone(newEntity) as CustomEmote;
		emote._name = _name;
		emote._texture = _texture;
		emote._category = _category;
		emote._activeCondition = _activeCondition;

		return emote;
	}

	/// <summary> Creates a new emote with an explicit name and texture. </summary>
	public CustomEmote(string name, string texture, int category, Func<bool> activeCondition)
	{
		_name = name;
		_texture = texture;
		_category = category;
		_activeCondition = activeCondition;
	}

	/// <summary> Creates a new emote with <see cref="Name"/> and <see cref="Texture"/> resulting from <paramref name="parentType"/>. </summary>
	public CustomEmote(Type parentType, int category, Func<bool> activeCondition)
	{
		_name = parentType.Name + "Emote";
		_texture = DrawHelpers.RequestLocal(parentType, _name);
		_category = category;
		_activeCondition = activeCondition;
	}

	/// <summary> Shorthand for <see cref="Mod.AddContent"/>. </summary>
	public static void LoadCustomEmote(CustomEmote emote) => SpiritReforgedMod.Instance.AddContent(emote);

	public override void SetStaticDefaults()
	{
		AddToCategory(_category);
		EmoteNPC.LoadedEmotes.Add(this);
	}

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