using Terraria.Audio;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

/// <summary> Controls backpack animations when attempting to move them, and inventory visuals when full. </summary>
internal partial class BackpackGlobal : GlobalItem
{
	public override bool InstancePerEntity => true;

	private float _visualCounter;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is BackpackItem;

	internal void StartAnimation()
	{
		SoundEngine.PlaySound(SoundID.Grab);
		_visualCounter = MathHelper.Min(_visualCounter + 1, 2.5f);
	}

	public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		if (_visualCounter > 0)
		{
			var texture = TextureAssets.Item[item.type].Value;
			float rotation = (float)Math.Sin(_visualCounter * 4f) * .3f;

			spriteBatch.Draw(texture, position, frame, Color.Black * .2f, rotation, origin, scale + _visualCounter * .15f, SpriteEffects.None, 0);
			spriteBatch.Draw(texture, position - new Vector2(0, _visualCounter * 5), frame, drawColor, rotation, origin, scale + _visualCounter * .15f, SpriteEffects.None, 0);

			_visualCounter = MathHelper.Max(_visualCounter - .075f, 0);

			return false;
		}

		return true;
	}

	public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
	{
		if (_visualCounter > 0 && line.Mod == "Terraria" && line.Name == "Tooltip1")
		{
			var color = Color.Lerp(Main.MouseTextColorReal, Color.White, _visualCounter);
			var position = new Vector2(line.X, line.Y) + (Vector2.UnitX * _visualCounter * 3).RotatedBy(Math.Sin(_visualCounter * 10f));

			Utils.DrawBorderString(Main.spriteBatch, line.Text, new Vector2(position.X, line.Y), Color.Black * _visualCounter * .2f);
			Utils.DrawBorderString(Main.spriteBatch, line.Text, position, color);

			return false;
		}

		return true;
	}
}
