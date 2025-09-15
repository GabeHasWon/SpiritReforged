using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.GameContent.Personalities;

namespace SpiritReforged.Content.Savanna.Biome;

public class SavannaBiome : ModBiome
{
	private int GetMusic()
	{
		if (Main.LocalPlayer.ZoneGraveyard || Main.bloodMoon)
			return -1;

		string name = Main.swapMusic ? "SavannaOtherworld" : "Savanna";
		return Main.dayTime ? MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}") : MusicLoader.GetMusicSlot(Mod, $"Assets/Music/{name}Night");
	}

	public override void SetStaticDefaults()
	{
		NPCHappinessHelper.SetAverage<SavannaBiome>(ModContent.GetInstance<JungleBiome>(), ModContent.GetInstance<DesertBiome>());

		NPCHappiness.Get(NPCID.BestiaryGirl).SetBiomeAffection(this, AffectionLevel.Like);
		NPCHappiness.Get(NPCID.ArmsDealer).SetBiomeAffection(this, AffectionLevel.Like);
		NPCHappiness.Get(NPCID.Stylist).SetBiomeAffection(this, AffectionLevel.Dislike);
		NPCHappiness.Get(NPCID.GoldBird).SetBiomeAffection(this, AffectionLevel.Dislike);

		SceneTileCounter.SurveyByType.Add(Type, new([ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassHallow>(), 
			ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrassMowed>(), ModContent.TileType<SavannaGrassHallowMowed>()], 400));
	}

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;
	public override float GetWeight(Player player) => 0.75f;

	public override int Music => GetMusic();
	public override ModWaterStyle WaterStyle => ModContent.GetInstance<SavannaWaterStyle>();
	public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<SavannaBGStyle>();
	public override string BackgroundPath => MapBackground;
	public override string MapBackground => "SpiritReforged/Assets/Textures/Backgrounds/SavannaMapBG";
	public override int BiomeTorchItemType => ModContent.ItemType<SavannaTorchItem>();
	public override int BiomeCampfireItemType => AutoContent.ItemType<SavannaCampfire>();

	public override bool IsBiomeActive(Player player)
	{
		bool surface = player.ZoneSkyHeight || player.ZoneOverworldHeight;
		return SceneTileCounter.SurveyByType[Type].Success && surface;
	}
}