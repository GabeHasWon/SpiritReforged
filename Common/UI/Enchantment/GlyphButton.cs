using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs;
using Terraria.Graphics.Renderers;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Enchantment;

public class GlyphButton : UIElement
{
	public readonly int itemType;
	private float _hoverTime;

	public GlyphButton(int itemType)
	{
		this.itemType = itemType;

		Width.Set(38, 0);
		Height.Set(38, 0);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (IsMouseHovering)
			_hoverTime = Math.Min(_hoverTime + 0.1f, 1);
		else
			_hoverTime = Math.Max(_hoverTime - 0.1f, 0);
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Texture2D outlineTexture = TextureColorCache.ColorSolid(texture, Color.White);
		Vector2 center = GetDimensions().Center() - new Vector2(0, _hoverTime * 4);

		if (IsMouseHovering)
		{
			spriteBatch.Draw(texture, GetDimensions().Center() + new Vector2(0, 2), null, Color.Black * 0.5f, 0, texture.Size() / 2, 1, 0, 0);
			Color outlineColor = ItemLoader.GetItem(itemType) is GlyphItem glyphItem ? glyphItem.settings.Color : Color.White;

			DrawHelpers.DrawOutline(spriteBatch, texture, center, Color.White, (offset) =>
				spriteBatch.Draw(outlineTexture, center + offset, null, outlineColor.Additive() * 0.5f, 0, texture.Size() / 2, 1, 0, 0));

			if ((int)Main.timeForVisualEffects % 80 == 0 || Main.rand.NextBool(120))
			{
				Vector2 velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);

				TerrariaParticles.OverInventory.Add(new PrettySparkleParticle()
				{
					LocalPosition = Main.rand.NextVector2FromRectangle(GetDimensions().ToRectangle()),
					Scale = new Vector2(Main.rand.NextFloat(0.25f, 0.6f)),
					ColorTint = outlineColor,
					Velocity = velocity,
					AccelerationPerFrame = -(velocity * 0.01f),
					TimeToLive = 120
				});
			}
		}
		else
		{
			DrawHelpers.DrawOutline(spriteBatch, texture, center, Color.White, (offset) =>
				spriteBatch.Draw(outlineTexture, center + offset, null, Color.Black * 0.3f, 0, texture.Size() / 2, 1, 0, 0));
		}

		spriteBatch.Draw(texture, center, null, Color.White, 0, texture.Size() / 2, 1, 0, 0);
	}
}