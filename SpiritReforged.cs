global using Terraria.ModLoader;
global using Terraria;
global using Terraria.ID;
global using Terraria.GameContent;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using ReLogic.Content;
global using System;
global using Terraria.Localization;
global using Terraria.Enums;
global using Terraria.ObjectData;
global using System.Collections.Generic;
global using NPCUtils;

using SpiritReforged.Common.ModCompat;
using Terraria.UI.Chat;

namespace SpiritReforged;

public partial class SpiritReforgedMod : Mod
{
	public const string ModName = "SpiritReforged";

	public static SpiritReforgedMod Instance { get; private set; }

	public SpiritReforgedMod()
	{
		Instance = this;
		PreAddContent.AddContentHook(this);
	}

	public override void Load()
	{
		RubbleAutoloader.Autoloader.Load(this);
		TileHelper.Autoloader.Load(this);

		NPCUtils.NPCUtils.AutoloadModBannersAndCritters(this);
        NPCUtils.NPCUtils.TryLoadBestiaryHelper(this);

#if DEBUG
		On_Main.DrawMenu += AddDebugInfo;
#endif
	}

	private void AddDebugInfo(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
	{
		orig(self, gameTime);

		Main.spriteBatch.Begin();

		// Used for clarifying when this was built and what branch it's for
		string text = $"Spirit Reforged v{Version} dev ({ThisAssembly.Git.Branch}) built ({ThisAssembly.Git.CommitDate})";
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, new Vector2(10), Color.White, Color.Black * 0.4f, 0f, Vector2.Zero, Vector2.One, -1, 2);

		Main.spriteBatch.End();
	}

	public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI) => Common.Multiplayer.MultiplayerHandler.HandlePacket(reader, whoAmI);
}