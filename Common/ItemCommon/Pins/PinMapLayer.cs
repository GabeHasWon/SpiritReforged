﻿using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon.Pins;

internal class PinMapLayer : ModMapLayer
{
	public static Dictionary<string, Asset<Texture2D>> Textures = null;

	private string heldPin = null;
	private float heldOffset;

	public override void Draw(ref MapOverlayDrawContext context, ref string text)
	{
		Textures ??= new Dictionary<string, Asset<Texture2D>>
		{
			{ "Blue", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinBlueMap") },
			{ "Red", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinRedMap") },
			{ "Green", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinGreenMap") },
			{ "Yellow", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinYellowMap") },
			{ "Tree", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinTreeMap") },
			{ "Star", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinStarMap") },
			{ "Copper", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinCopperMap") },
			{ "Silver", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinSilverMap") },
			{ "Gold", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinGoldMap") },
			{ "Scarab", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinScarabMap") },
			{ "Moonjelly", ModContent.Request<Texture2D>($"SpiritMod/Items/Pins/Textures/PinMoonjellyMap") },
		};

		var pins = ModContent.GetInstance<PinWorld>().pins;
		bool placedPin = false;

		if (heldPin != null)
			HoldPin(ref placedPin);

		foreach (var pair in pins)
		{
			var pos = pins.Get<Vector2>(pair.Key);
			float scale = 1f + (heldPin == pair.Key ? heldOffset * .05f : 0);

			if (context.Draw(Textures[pair.Key].Value, pos, Color.White, new SpriteFrame(1, 1, 0, 0), scale, scale, Alignment.Center).IsMouseOver)
			{
				if (!Main.mapFullscreen)
					continue;

				if (Main.mouseLeft && Main.mouseLeftRelease && !placedPin)
					heldPin = pair.Key;

				if (Main.mouseRight && Main.mouseRightRelease)
					ModContent.GetInstance<PinWorld>().RemovePin(pair.Key);

				if (heldPin == null)
					text = Language.GetTextValue("Mods.SpiritMod.Misc.PinMove");
			}
		}
	}

	private void HoldPin(ref bool placedPin)
	{
		float heldOffsetMax = 4f;
		string heldPinValue = heldPin;

		heldOffset = MathHelper.Lerp(heldOffset, heldOffsetMax, 0.2f);

		if (Main.mouseLeft && Main.mouseLeftRelease || !Main.mapFullscreen) //Drop the pin
		{
			heldOffset = 0;
			heldPin = null;
			placedPin = true;

			if (Main.netMode != NetmodeID.Server)
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Items/MapPin") with { PitchVariance = 0.3f });
		}

		var drawOffset = new Vector2(0, heldOffset); //Hover above the cursor slightly when held
		Vector2 cursorPos = Main.MouseScreen - Main.ScreenSize.ToVector2() / 2;
		cursorPos = (cursorPos - drawOffset) * (1 / Main.mapFullscreenScale) + Main.mapFullscreenPos;

		// TODO
		//if (placedPin && Main.netMode == NetmodeID.MultiplayerClient)
		//{
		//	ModPacket packet = SpiritMod.Instance.GetPacket(MessageType.PlaceMapPin, 3);
		//	packet.Write(cursorPos.X);
		//	packet.Write(cursorPos.Y);
		//	packet.Write(heldPinValue);
		//	packet.Send();
		//}

		ModContent.GetInstance<PinWorld>().SetPin(heldPinValue, cursorPos);
	}
}
