using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace SpiritReforged.Common.UI;

public class UILinkNodeVisualizer : ModSystem
{
	private static bool _toggle = false;
	private static int _timer = 0;

	public override void Load() => On_Main.DoDraw += DrawHijack;

	private void DrawHijack(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
	{
		orig(self, gameTime);

		_timer--;

		if (Keyboard.GetState().IsKeyDown(Keys.OemTilde) && _timer < 0)
		{
			_timer = 30;
			_toggle = !_toggle;
		}

		if (!_toggle)
			return;

		int hiddenCount = 0;

		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

		foreach (var pair in UILinkPointNavigator.Points)
		{
			if (pair.Value is not null)
			{
				if (pair.Value?.Position is { } pos && pos != Vector2.Zero)
				{
					int id = pair.Key;
					UILinkPoint point = pair.Value;

					Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)point.Position.X, (int)point.Position.Y, 8, 8), Color.White);
					Color color = point.Enabled ? Color.White : Color.Red;
					ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, id.ToString(), point.Position, color, 0, Vector2.Zero, Vector2.One);

					if (pair.Key == UILinkPointNavigator.CurrentPoint)
					{
						Vector2 position = point.Position + new Vector2(250, 0);
						string connections = $"u:{point.Up} d:{point.Down} l:{point.Left} r:{point.Right}";
						ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, connections, position, color, 0, Vector2.Zero, Vector2.One);
					}
				}
				else
					hiddenCount++;
			}
		}

		PlayerInput.SetZoom_Unscaled();
		string text = "Hidden points:" + hiddenCount;
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, Main.MouseScreen, Color.White, 0, Vector2.Zero, Vector2.One);
		PlayerInput.SetZoom_World();

		Main.spriteBatch.End();
	}
}
