using Terraria.UI.Chat;

namespace SpiritReforged.Common.NPCCommon;

public interface INPCButtons
{
	public ButtonText[] AddButtons();

	public void OnClickButton(ButtonText button);
}

internal class AdditionalShopButtonEdits : ModSystem
{
	public override void Load() => On_Main.DrawNPCChatButtons += AddNewButtons;

	private void AddNewButtons(On_Main.orig_DrawNPCChatButtons orig, int superColor, Color chatColor, int numLines, string focusText, string focusText3)
	{
		orig(superColor, chatColor, numLines, focusText, focusText3);

		int x = 180 + (Main.screenWidth - 800) / 2;
		float y = 130 + numLines * 30;
		var basePosition = new Vector2(x + 380, y);
		var font = FontAssets.MouseText.Value;

		NPC npc = Main.LocalPlayer.TalkNPC;

		if (npc is not null && ModContent.GetModNPC(npc.type) is INPCButtons npcButtons)
		{
			ButtonText[] buttonStrings = npcButtons.AddButtons();
			float adjX = 0;

			for (int i = 0; i < buttonStrings.Length; ++i)
			{
				ButtonText button = buttonStrings[i];
				Vector2 stringSize = ChatManager.GetStringSize(font, button.Text, new(0.9f));
				Vector2 pos = basePosition + stringSize * 0.5f + new Vector2(adjX, 0);
				Vector2 boundsPos = pos - stringSize / 2f;
				Rectangle rect = new Rectangle((int)boundsPos.X, (int)boundsPos.Y, (int)stringSize.X, (int)stringSize.Y);
				bool hover = rect.Contains(Main.MouseScreen.ToPoint());
				Vector2 scale = new Vector2(0.9f) * (hover ? 1.2f : 1f);

				if (hover && Main.mouseLeft && Main.mouseLeftRelease)
					npcButtons.OnClickButton(button);

				ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, button.Text, pos, chatColor, hover ? Color.Brown : Color.Black, 0f, stringSize * 0.5f, scale);
				adjX -= stringSize.X + 4;
			}
		}
	}
}

public readonly struct ButtonText(string name, string text)
{
	public readonly string Name = name;
	public readonly string Text = text;
}