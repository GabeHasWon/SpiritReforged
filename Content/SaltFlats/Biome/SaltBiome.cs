using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.SaltFlats.Tiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.GameContent.Personalities;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBiome : ModBiome
{
	private int GetMusic()
	{
		if (Main.LocalPlayer.ZoneGraveyard || Main.bloodMoon)
			return -1;

		string name = "Salt"; //SpiritReforgedMod.SwapMusic ? "SaltOtherworld" : "Salt";
		return Main.dayTime ? MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}") : MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}Night");
	}

	public override void Load()
	{
		On_Player.ManageSpecialBiomeVisuals += NoHeatHazeInFlats;
	}

	private void NoHeatHazeInFlats(On_Player.orig_ManageSpecialBiomeVisuals orig, Player self, string biomeName, bool inZone, Vector2 activationSource)
	{
		//Disable heat haze when in salt flats (and not in hell)
		if (inZone && biomeName == "HeatDistortion" && self.InModBiome<SaltBiome>() && activationSource.Y < Main.maxTilesY - 400)
			inZone = false;

		orig(self, biomeName, inZone, activationSource);
	}

	public override void SetStaticDefaults()
	{
		NPCHappinessHelper.SetAverage<SaltBiome>(ModContent.GetInstance<SnowBiome>(), ModContent.GetInstance<DesertBiome>());

		NPCHappiness.Get(NPCID.Wizard).SetBiomeAffection(this, AffectionLevel.Like);
		NPCHappiness.Get(NPCID.Painter).SetBiomeAffection(this, AffectionLevel.Like);
		NPCHappiness.Get(NPCID.Truffle).SetBiomeAffection(this, AffectionLevel.Dislike);
		NPCHappiness.Get(NPCID.Mechanic).SetBiomeAffection(this, AffectionLevel.Dislike);
		NPCHappiness.Get(NPCID.Steampunker).SetBiomeAffection(this, AffectionLevel.Dislike);

		SceneTileCounter.SurveyByType.Add(Type, new([ModContent.TileType<SaltBlockReflective>(), ModContent.TileType<SaltBlockDull>()], 900));
	}

	public override ModWaterStyle WaterStyle => ModContent.GetInstance<SaltWaterStyle>();
	public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<SaltBGStyle>();
	public override int BiomeTorchItemType => ModContent.ItemType<SaltTorchItem>();
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override string MapBackground => "SpiritReforged/Assets/Textures/Backgrounds/SaltFlatsMapBG";
	public override string BackgroundPath => MapBackground;
	public override int Music => GetMusic();

	public override bool IsBiomeActive(Player player)
	{
		bool surface = player.ZoneSkyHeight || player.ZoneOverworldHeight;
		return SceneTileCounter.SurveyByType[Type].Success && surface;
	}

	public override void OnInBiome(Player player)
	{
		player.ZoneDesert = true;
		player.ZoneSnow = true;
		player.ZoneSandstorm = false;
	}
}