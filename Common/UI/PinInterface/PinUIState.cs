using SpiritReforged.Common.ItemCommon.Pins;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Common.Visuals;
using Terraria.GameInput;
using Terraria.UI;

namespace SpiritReforged.Common.UI.PinInterface;

internal class PinUIState : AutoUIState
{
	private static Asset<Texture2D> buttonTexture;

	private UIElement openButton;
	private bool _listActive = false;

	public override void OnInitialize()
	{
		Width = Height = StyleDimension.Fill;

		buttonTexture = ModContent.Request<Texture2D>(GetType().Namespace.Replace(".", "/") + "/OpenButton");

		openButton = new();
		openButton.Width.Set(30, 0);
		openButton.Height.Set(30, 0);
		openButton.Top.Set(-openButton.Height.Pixels - 14, 1); //Position next to the 'close map' button
		openButton.Left.Set(openButton.Width.Pixels + 14, 0);
		openButton.OnLeftClick += ToggleList;
		Append(openButton);

		Main.OnPostFullscreenMapDraw += ForceDraw;
	}

	public override void Unload(Mod mod) => Main.OnPostFullscreenMapDraw -= ForceDraw;

	private void ToggleList(UIMouseEvent evt, UIElement listeningElement) => SetStorageSlots(!(_listActive = !_listActive));

	private void ForceDraw(Vector2 arg1, float arg2) //By default, UI doesn't draw when the fullscreen map is open
	{
		if (WorldGen.generatingWorld)
			return;

		UISystem.SetActive<PinUIState>();

		int oldMouseX = Main.mouseX;
		int oldMouseY = Main.mouseY;

		PlayerInput.SetZoom_UI();

		Main.spriteBatch.Begin(default, default, default, default, default, default, Main.UIScaleMatrix);

		if (UISystem.IsActive<PinUIState>())
		{
			UserInterface.CurrentState?.Update(new GameTime());
			UserInterface.CurrentState?.Draw(Main.spriteBatch);
		}

		Main.spriteBatch.End();

		Main.mouseX = oldMouseX;
		Main.mouseY = oldMouseY;
	}

	public override void Update(GameTime gameTime)
	{
		if (!Main.mapFullscreen)
		{
			SetStorageSlots(true);
			_listActive = false;

			UISystem.SetInactive<PinUIState>(); //Close the UI
			return;
		}

		base.Update(gameTime);
	}

	private void SetStorageSlots(bool clear)
	{
		if (clear)
		{
			List<UIElement> removals = [];

			foreach (var item in Children)
				if (item is PinUISlot)
					removals.Add(item);

			foreach (var item in removals)
				RemoveChild(item);
		}
		else
		{
			int count = 0;
			foreach (string name in PinSystem.DataByName.Keys)
			{
				Append(new PinUISlot(name)
				{
					Left = new StyleDimension(openButton.Left.Pixels + 32 + count * 32, 0),
					Top = new StyleDimension(openButton.Top.Pixels + 2, openButton.Top.Percent)
				});

				count++;
			}
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		bool hasNewPin = Main.LocalPlayer.GetModPlayer<PinPlayer>().newPins.Count != 0;
		if (openButton.IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			DrawButton(Color.White, 1);
		}
		else
		{
			DrawButton(hasNewPin ? Color.White : Color.White * .5f, 0);
		}

		void DrawButton(Color color, int frame)
		{
			var position = openButton.GetDimensions().Center() - new Vector2(buttonTexture.Width() / 2, buttonTexture.Height() / 4);
			var source = buttonTexture.Frame(1, 2, 0, frame, sizeOffsetY: -2);

			if (hasNewPin)
			{
				float lerp = (float)Math.Sin(Main.timeForVisualEffects / 20f);
				var glowPos = openButton.GetDimensions().Center() + new Vector2(0, 4);

				spriteBatch.Draw(AssetLoader.LoadedTextures["Bloom"].Value, glowPos, null, 
					(Color.White with { A = 0 }) * (.5f - lerp * .025f), 0, AssetLoader.LoadedTextures["Bloom"].Size() / 2, .25f - lerp * .025f, default, 0);

				spriteBatch.Draw(AssetLoader.LoadedTextures["GodrayCircle"].Value, glowPos, null,
					(Color.Orange with { A = 0 }) * (.5f - lerp * .025f), (float)Main.timeForVisualEffects / 120, AssetLoader.LoadedTextures["GodrayCircle"].Size() / 2, .1f - lerp * .01f, default, 0);

				DrawHelpers.DrawGodrays(Main.spriteBatch, glowPos, Color.Goldenrod with { A = 0 }, 22, 10, 5);
				DrawHelpers.DrawGodrays(Main.spriteBatch, glowPos, Color.White with { A = 0 }, 16, 5, 5);
			}

			spriteBatch.Draw(buttonTexture.Value, position, source, color);
		}
	}
}
