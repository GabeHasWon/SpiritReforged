using SpiritReforged.Common.ItemCommon.Pins;
using Terraria.Audio;
using Terraria.UI;

namespace SpiritReforged.Common.UI.PinInterface;

public class PinUISlot : UIElement
{
	public bool Highlighted { get; private set; }

	private static Asset<Texture2D> shadowTexture;

	public const int Context = ItemSlot.Context.ChestItem;
	public const float Scale = 1f;

	private readonly string _name;
	private readonly bool _unlocked;

	private float _offset;
	private float _fadein;

	public PinUISlot(string name)
	{
		_unlocked = Main.LocalPlayer.PinUnlocked(name);
		Highlighted = Main.LocalPlayer.GetModPlayer<PinPlayer>().newPins.Contains(name);
		_name = name;

		Width = Height = new StyleDimension(52 * .5f * Scale, 0f);

		shadowTexture ??= ModContent.Request<Texture2D>(GetType().Namespace.Replace(".", "/") + "/Shadow");
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		var item = PinSystem.DataByName[_name].Item;

		var center = GetDimensions().Center();
		var fadeOffset = Vector2.UnitX * (float)Math.Sin((1f - _fadein) * 1.5f) * 5f;
		float opacity = _fadein * (ModContent.GetInstance<PinSystem>().pins.ContainsKey(_name) ? .5f : 1f);

		spriteBatch.Draw(shadowTexture.Value, center + new Vector2(0, 12) + fadeOffset, null, Color.White * .5f * opacity, 0, shadowTexture.Size() / 2, Scale, SpriteEffects.None, 0);

		if (_unlocked)
		{
			DrawOutline(item);
			ItemSlot.DrawItemIcon(item, Context, spriteBatch, center - new Vector2(0, _offset * 3f) + fadeOffset, Scale, 32f, Color.White * opacity);

			if (Highlighted)
				spriteBatch.Draw(TextureAssets.QuicksIcon.Value, GetDimensions().Position(), null, Main.MouseTextColorReal, 0, 
					TextureAssets.QuicksIcon.Size() / 2, Main.mouseTextColor / 255f, SpriteEffects.None, 0);
		}

		if (_unlocked)
			HandleItemSlotLogic();

		_fadein = MathHelper.Min(_fadein + .1f, 1);

		void DrawOutline(Item item)
		{
			for (int i = 0; i < 4; i++)
			{
				Vector2 outlineOffset = i switch
				{
					0 => new Vector2(2, 0),
					1 => new Vector2(0, 2),
					2 => new Vector2(-2, 0),
					3 => new Vector2(0, -2),
					_ => Vector2.Zero,
				};

				ItemSlot.DrawItemIcon(item, Context, spriteBatch, center - new Vector2(0, _offset * 3f) + outlineOffset + fadeOffset, Scale, 32f, Color.Black * .25f * opacity);
			}
		}
	}

	private void HandleItemSlotLogic()
	{
		if (!IsMouseHovering)
		{
			_offset = MathHelper.Max(_offset - .2f, 0);
			return;
		}

		if (_offset == 0) //Just started hovering
			SoundEngine.PlaySound(SoundID.MenuTick);

		if (Highlighted)
		{
			Highlighted = false;
			Main.LocalPlayer.GetModPlayer<PinPlayer>().newPins.Remove(_name);
		}

		if (Main.mouseLeft && Main.mouseLeftRelease)
		{
			PinSystem.Place(_name, Vector2.Zero);
			PinMapLayer.HoldPin(_name);
			SoundEngine.PlaySound(SoundID.Grab);
		}

		Main.LocalPlayer.mouseInterface = true;
		_offset = MathHelper.Min(_offset + .2f, 1f);
	}
}
