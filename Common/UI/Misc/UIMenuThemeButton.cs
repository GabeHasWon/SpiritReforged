using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SpiritReforged.Common.UI.Misc;

public class UIMenuThemeButton : UIElement
{
	private static FieldInfo SwitchToMenu;
	private static FieldInfo CurrentMenu;
	private static ModMenu LastMenu;

	private readonly Asset<Texture2D> Texture;
	private float _scale = 1f;
	private bool _wasMouseHovering;

	/// <summary> Prevents interacting with the button if our theme is already selected. </summary>
	private bool _themeSelected;

	/// <summary> Checks whether required fields exist. Should be called before this element is appended. </summary>
	/// <returns> Whether the required fields were found using reflection. </returns>
	public static bool CanExist()
	{
		SwitchToMenu = typeof(MenuLoader).GetField("switchToMenu", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);
		CurrentMenu = typeof(MenuLoader).GetField("currentMenu", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic);

		return SwitchToMenu != null && CurrentMenu != null;
	}

	public UIMenuThemeButton(Asset<Texture2D> texture)
	{
		//Check whether the savanna theme is currently selected when the mod menu opens
		_themeSelected = CurrentMenu.GetValue(null) is ModMenu menu && menu == ModContent.GetInstance<SavannaMenuTheme>();

		Texture = texture;
		Width.Set(30, 0);
		Height.Set(30, 0);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		const float maxScaleIncrease = .1f;

		var pos = GetDimensions().Center();
		float opacity = .5f;

		if (IsMouseHovering && !_themeSelected)
		{
			UICommon.TooltipMouseText(Language.GetTextValue("Mods.SpiritReforged.Misc.MenuButton"));

			if (Main.mouseLeft && Main.mouseLeftRelease) //Confirm the theme
			{
				SwitchToMenu.SetValue(null, ModContent.GetInstance<SavannaMenuTheme>());
				SoundEngine.PlaySound(SoundID.MenuOpen);
				_themeSelected = true;
			}

			_scale = Math.Min(_scale + .01f, 1 + maxScaleIncrease);
			opacity = 1;

			float fadeIn = (_scale - 1f) / maxScaleIncrease;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			spriteBatch.Draw(bloom, pos, null, (Color.Cyan * fadeIn).Additive(), 0, bloom.Size() / 2, .25f, default, 0);

			spriteBatch.Draw(Texture.Value, pos, null, (Color.Cyan * fadeIn).Additive(), 0, Texture.Size() / 2, _scale * 1.05f, default, 0);

			if (!_wasMouseHovering) //While hovering, change the theme for a quick preview
			{
				if (CurrentMenu.GetValue(null) is ModMenu menu)
					LastMenu = menu;

				SwitchToMenu.SetValue(null, ModContent.GetInstance<SavannaMenuTheme>());
			}
		}
		else
		{
			_scale = Math.Max(_scale - .01f, 1f);

			if (!_themeSelected && _wasMouseHovering) //If our theme wasn't actually selected (but being previewed instead), then change it back
				SwitchToMenu.SetValue(null, LastMenu);
		}

		spriteBatch.Draw(Texture.Value, pos, null, Color.White * opacity, 0, Texture.Size() / 2, _scale, default, 0);
		_wasMouseHovering = IsMouseHovering;
	}
}