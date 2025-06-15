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
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.ModCompat;
using System.Runtime.CompilerServices;

namespace SpiritReforged;

public partial class SpiritReforgedMod : Mod
{
	public const string ModName = "SpiritReforged";

	public static SpiritReforgedMod Instance { get; private set; }

	/// <summary>
	/// Gets if Otherworld Music is turned on. <see cref="Main.swapMusic"/> is private for some reason.
	/// </summary>
	public static bool SwapMusic => GetSwapMusic(null);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "swapMusic")]
	private static extern ref bool GetSwapMusic(Main main);

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

		TrailDetours.Initialize();

		AssetLoader.Load(this);

		ParticleHandler.RegisterParticles();
		ParticleDetours.Initialize();
	}

	public override void Unload()
	{
		NPCUtils.NPCUtils.UnloadMod(this);
		NPCUtils.NPCUtils.UnloadBestiaryHelper();
		AssetLoader.Unload();
		TrailDetours.Unload();

		ParticleHandler.Unload();
		ParticleDetours.Unload();
	}

	public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI) => Common.Multiplayer.MultiplayerHandler.HandlePacket(reader, whoAmI);
}