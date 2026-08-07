using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;

namespace SpiritReforged.Content.Underground.Moss;

public class NeonMossScene : ModSceneEffect
{
	public static bool InNeonMoss
	{
		get
		{
			int type = ModContent.GetInstance<NeonMossScene>().Type;
			if (!SceneTileCounter.SurveyByType.ContainsKey(type)) //DEBUG- scene will not load, for some reason!
			{
				SceneTileCounter.SurveyByType.Add(type, new([TileID.ArgonMoss, TileID.KryptonMoss,
					TileID.XenonMoss, TileID.VioletMoss, TileID.RainbowMoss, ModContent.TileType<RadonMoss>(), ModContent.TileType<OganessonMoss>()], 200));
			}
			
			return SceneTileCounter.SurveyByType.TryGetValue(type, out var survey) && survey.Success;
		}
	}

	public override SceneEffectPriority Priority => SceneEffectPriority.Environment;

	public override void SetStaticDefaults() => SceneTileCounter.SurveyByType.Add(Type, new([TileID.ArgonMoss, TileID.KryptonMoss, 
		TileID.XenonMoss, TileID.VioletMoss, TileID.RainbowMoss, ModContent.TileType<RadonMoss>(), ModContent.TileType<OganessonMoss>()], 200));

	public override bool IsSceneEffectActive(Player player) => InNeonMoss;
}