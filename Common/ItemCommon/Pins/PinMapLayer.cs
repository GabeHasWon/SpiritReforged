using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon.Pins;

internal class PinMapLayer : ModMapLayer
{
	public static readonly SoundStyle MapPin = new("SpiritReforged/Assets/SFX/Item/MapPin")
	{
		PitchVariance = .3f
	};

	private string _heldPinName;
	private float _heldOffset;

	/// <summary> Holds a pin of <paramref name="name"/> on the cursor. </summary>
	/// <returns> Whether this pin exists. </returns>
	public static bool HoldPin(string name)
	{
		var pins = ModContent.GetInstance<PinSystem>().pins;

		if (pins.ContainsKey(name))
		{
			ModContent.GetInstance<PinMapLayer>()._heldPinName = name;
			return true;
		}
		
		return false;
	}

	public override void Draw(ref MapOverlayDrawContext context, ref string text)
	{
		var pins = ModContent.GetInstance<PinSystem>().pins;

		foreach (var pair in pins)
		{
			string name = pair.Key;
			var position = pins.Get<Vector2>(name);

			DrawPin(ref context, ref text, name, position);
		}
	}

	private void DrawPin(ref MapOverlayDrawContext context, ref string text, string name, Vector2 position)
	{
		if (!PinSystem.DataByName.TryGetValue(name, out var data))
			return;

		Texture2D texture = data.Texture.Value;
		float scale = 1;
		UpdatePin(name, ref position, ref scale, ref text); //Adjusts position and scale of held pins

		if (context.Draw(texture, position, Color.White, new SpriteFrame(1, 1, 0, 0), scale, scale, Alignment.Bottom).IsMouseOver)
		{
			if (!Main.mapFullscreen)
				return;

			UpdatePin(name, ref position, ref scale, ref text, true);
		}
	}

	private void UpdatePin(string name, ref Vector2 position, ref float scale, ref string text, bool hovering = false)
	{
		bool holdingThisPin = _heldPinName == name;

		if (holdingThisPin)
		{
			position = GetCursor();
			_heldOffset = MathHelper.Min(_heldOffset + .1f, 1);
			scale = 1 + _heldOffset * .15f;

			if (Main.mouseLeft && Main.mouseLeftRelease) //Pick up
			{
				_heldPinName = string.Empty;
				_heldOffset = 0;
				PinSystem.Place(name, GetCursor());

				SoundEngine.PlaySound(MapPin);
			}
		}
		else if (hovering)
		{
			text = Language.GetTextValue("Mods.SpiritReforged.Misc.Pins.Move");

			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				_heldPinName = name;
				SoundEngine.PlaySound(SoundID.Grab);
			}

			if (Main.mouseRight)
			{
				PinSystem.Remove(name);
				SoundEngine.PlaySound(SoundID.Grab);
			}
		}

		Vector2 GetCursor()
		{
			var cursorPos = Main.MouseScreen - Main.ScreenSize.ToVector2() / 2;
			cursorPos = (cursorPos - new Vector2(0, _heldOffset * 5)) * (1 / Main.mapFullscreenScale) + Main.mapFullscreenPos;

			return cursorPos;
		}
	}
}
