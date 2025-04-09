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

using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged;

public partial class SpiritReforgedMod : Mod
{
	public static SpiritReforgedMod Instance { get; private set; }

	public SpiritReforgedMod()
	{
		GoreAutoloadingEnabled = true;
		Instance = this;

		PreAddContent.AddContentHook(this);
	}

	public override void Load()
	{
		CustomSapling.Autoload(this);
		RubbleAutoloader.Autoloader.Load(this);
		NPCUtils.NPCUtils.AutoloadModBannersAndCritters(this);
		NPCUtils.NPCUtils.TryLoadBestiaryHelper();
		AutoloadMinionDictionary.AddBuffs(Code);
		
		TrailDetours.Initialize();

		AssetLoader.Load(this);

		ParticleHandler.RegisterParticles();
		ParticleDetours.Initialize();
	}

	public override void Unload()
	{
		NPCUtils.NPCUtils.UnloadMod(this);
		NPCUtils.NPCUtils.UnloadBestiaryHelper();
		AutoloadMinionDictionary.Unload();
		AssetLoader.Unload();
		TrailDetours.Unload();

		ParticleHandler.Unload();
		ParticleDetours.Unload();
	}

	public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI) => Common.Multiplayer.MultiplayerHandler.HandlePacket(reader, whoAmI);
}