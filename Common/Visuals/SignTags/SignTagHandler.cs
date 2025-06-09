using ILLogger;
using MonoMod.Cil;
using Terraria.GameInput;

namespace SpiritReforged.Common.Visuals.CustomText;

internal class SignTagHandler : ILoadable
{
	private delegate void TagAction(SignTag tag);

	public static bool HasTag => _currentTag is not null;

	private static readonly HashSet<SignTag> loadedTags = [];
	/// <summary> Whether a sign was being hovered over. </summary>
	private static bool _wasSignHover;
	/// <summary> All tag data on the currently viewed sign. </summary>
	private static string _currentTag;

	public void Load(Mod mod)
	{
		foreach (var type in GetType().Assembly.GetTypes())
		{
			if (type.IsSubclassOf(typeof(SignTag)) && !type.IsAbstract)
				loadedTags.Add((SignTag)Activator.CreateInstance(type));
		}

		On_Main.DrawMouseOver += TrackSignText;
		IL_Main.DrawMouseOver += ModifySignHover;
		On_Main.TextDisplayCache.PrepareCache += ModifySignMenu;
		On_Sign.TextSign += VerifyEditTag;
		On_Main.DrawInterface += ResetSignHover;
	}

	private static void TrackSignText(On_Main.orig_DrawMouseOver orig, Main self)
	{
		if (!Main.mouseText && !Main.LocalPlayer.mouseInterface && Main.signHover != -1)
		{
			var sign = Main.sign[Main.signHover];
			if (sign != null)
			{
				string oldText = sign.text;

				if (!_wasSignHover || Main.LocalPlayer.sign != -1)
					VerifyTags(oldText);

				if (HasTag)
				{
					if (sign.text.Length >= _currentTag.Length)
						sign.text = sign.text.Remove(0, _currentTag.Length); //Remove the special tag before drawing
					else
						_currentTag = null; //The current tag is somehow invalid

					orig(self);

					sign.text = oldText; //Revert
					return;
				}
			}
		}

		orig(self);
	}

	private static void GetText(string tag, TagAction action)
	{
		foreach (var loaded in loadedTags)
		{
			if (tag.Contains(loaded.Key))
				action?.Invoke(loaded);
		}
	}

	/// <summary> Verifies whether the given text contains any <see cref="SignTag.Key"/>s and assigns <see cref="_currentTag"/>. <para/>
	/// Additionally parses parameter data corresponding to the current <see cref="loadedTags"/>. </summary>
	/// <param name="signText"> The sign text. </param>
	private static void VerifyTags(string signText)
	{
		const char close = '>';
		const char paramsIndicator = ':';

		_currentTag = null; //Reset

		while (true)
		{
			int fails = 0;

			foreach (var sig in loadedTags)
			{
				string key = $"<{sig.Key}";
				int length = Math.Min(key.Length, signText.Length);

				if (StartIndex() < signText.Length - length && signText.IndexOf(key, StartIndex(), length) == StartIndex())
				{
					string addTo = ProcessTag(key);

					if (addTo is not null && (_currentTag + addTo).Length < signText.Length)
					{
						_currentTag += addTo;
						break; //Return to the previous loop and search for another tag
					}
				}

				fails++;
			}

			if (fails == loadedTags.Count)
				break;
		}

		string ProcessTag(string key)
		{
			int paramStartIndex = StartIndex() + key.Length;
			string innerTag = null;

			if (signText[paramStartIndex] != paramsIndicator) //No parameter indicator
			{
				innerTag = key + close;
				if (signText.IndexOf(innerTag, StartIndex()) == -1) //Check for 'close'
					return null;

				GetText(innerTag, delegate (SignTag tag) { tag.AddParameters(null); });
			}
			else //Appears to have parameters; try to parse them
			{
				string paramsText = string.Empty;
				for (int i = paramStartIndex + 1; i < signText.Length; i++)
				{
					if (signText[i] == close)
						break;
					paramsText += signText[i];
				}

				innerTag = key + paramsIndicator + paramsText + close;

				bool isNull = false;
				GetText(innerTag, delegate (SignTag tag)
				{
					if (tag.AddParameters(paramsText) is not true)
						isNull = true;
				});

				if (isNull)
					return null;
			}

			return innerTag;
		}

		int StartIndex() => _currentTag?.Length ?? 0;
	}

	private static void ModifySignHover(ILContext il)
	{
		ILCursor c = new(il);
		ILLabel label = null;

		if (!c.TryGotoNext(x => x.MatchCallvirt<SpriteBatch>("End")))
		{
			SpiritReforgedMod.Instance.LogIL("Modify Sign Hover", "Method 'SpriteBatch.End' not found.");
			return;
		}

		if (!c.TryGotoPrev(MoveType.After, x => x.MatchBrtrue(out label)))
		{
			SpiritReforgedMod.Instance.LogIL("Modify Sign Hover", "IL Instruction 'Brtrue' not found.");
			return;
		}

		if (label is null)
		{
			SpiritReforgedMod.Instance.LogIL("Modify Sign Hover", "Label was null.");
			return;
		}

		c.EmitDelegate(CheckHasTag);
		c.EmitBrfalse(label);
	}

	/// <summary> Calculates custom sign text drawing and passes the data into the <see cref="loadedTags"/> corresponding to <see cref="_currentTag"/>. </summary>
	/// <returns> Whether to draw normal sign text. </returns>
	private static bool CheckHasTag()
	{
		if (HasTag) //Abbreviated vanilla code
		{
			string[] array = Utils.WordwrapString(Main.sign[Main.signHover].text, FontAssets.MouseText.Value, 460, 10, out int lineAmount);
			lineAmount++;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, null, RasterizerState.CullNone, null, Main.UIScaleMatrix);

			PlayerInput.SetZoom_UI();

			var screenSize = Main.ScreenSize;
			var mouse = Main.MouseScreen;

			PlayerInput.SetZoom_UI();
			PlayerInput.SetZoom_Test();
			float width = 0;

			for (int k = 0; k < lineAmount; k++)
			{
				float x = FontAssets.MouseText.Value.MeasureString(array[k]).X;
				width = Math.Max(width, x);
			}

			width = Math.Min(width, 460);
			var vector = mouse + new Vector2(16);

			if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
				vector += new Vector2(8, 2);

			vector = Vector2.Min(vector, new Vector2(screenSize.X - width, screenSize.Y - 30 * lineAmount));
			var rectangle = new Rectangle((int)vector.X - 10, (int)vector.Y - 5, (int)width + 20, 30 * lineAmount + 7);
			var color = Main.MouseTextColorReal;

			if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
			{
				color = Color.Lerp(color, Color.White, 1);
				Utils.DrawInvBG(Main.spriteBatch, rectangle, new Color(23, 25, 81, 255) * 0.925f * 0.85f);
			}

			bool skipDraw = false;
			GetText(_currentTag, delegate (SignTag tag)
			{
				if (tag.Draw(rectangle, array, lineAmount, ref color) == true)
					skipDraw = true;
			});

			if (!skipDraw)
			{
				var textPosition = new Vector2(rectangle.X + 10, rectangle.Y + 5);
				for (int line = 0; line < lineAmount; line++)
					Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, array[line], textPosition.X, textPosition.Y + line * 30, color, Color.Black, Vector2.Zero);
			}

			Main.mouseText = true;
			return false;
		}

		return true;
	}

	/// <summary> Removes the current tag text (<see cref="_currentTag"/>) from sign dialogue when interacting with one. </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="text"> The cached <see cref="Main.npcChatText"/> value. </param>
	/// <param name="baseColor"> The color of the text. </param>
	private static void ModifySignMenu(On_Main.TextDisplayCache.orig_PrepareCache orig, object self, string text, Color baseColor)
	{
		if (HasTag && !Main.editSign && Main.LocalPlayer.sign != -1)
		{
			if (_wasSignHover)
				VerifyTags(text);

			text = text.Remove(0, Math.Min(_currentTag.Length, text.Length));
		}

		orig(self, text, baseColor);
	}

	/// <summary> Updates <see cref="_currentTag"/> when finished editing a sign. </summary>
	/// <param name="orig"></param>
	/// <param name="i"> The index of the sign in <see cref="Main.sign"/>. </param>
	/// <param name="text"> The sign text. </param>
	private static void VerifyEditTag(On_Sign.orig_TextSign orig, int i, string text)
	{
		orig(i, text);
		VerifyTags(text);
	}

	private static void ResetSignHover(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
	{
		orig(self, gameTime);
		_wasSignHover = Main.signHover != -1;
	}

	public void Unload() { }
}
