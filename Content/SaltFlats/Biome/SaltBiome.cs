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
	public override int BiomeTorchItemType => ModContent.ItemType<SaltFlatsTorchItem>();
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;
	public override int Music => GetMusic();

	public override bool IsBiomeActive(Player player)
	{
		bool surface = player.ZoneSkyHeight || player.ZoneOverworldHeight;
		return SceneTileCounter.SurveyByType[Type].Success && surface;
	}
}