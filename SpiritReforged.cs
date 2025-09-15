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
		NPCUtils.NPCUtils.AutoloadModBannersAndCritters(this);
		NPCUtils.NPCUtils.TryLoadBestiaryHelper();
	}

	public override void Unload()
	{
		NPCUtils.NPCUtils.UnloadMod(this);
		NPCUtils.NPCUtils.UnloadBestiaryHelper();
	}

	public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI) => Common.Multiplayer.MultiplayerHandler.HandlePacket(reader, whoAmI);
}