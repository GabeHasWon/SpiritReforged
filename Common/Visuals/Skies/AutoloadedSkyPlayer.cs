using Terraria.Graphics.Effects;

namespace SpiritReforged.Common.Visuals.Skies;

public class AutoloadedSkyPlayer : ModPlayer
{
	public override void PostUpdateMiscEffects()
	{
		if (Main.dedServ || Player.whoAmI != Main.myPlayer)
			return; //Only run locally

		foreach (string key in AutoloadSkyDict.LoadedSkies.Keys)
		{
			bool skyIsActive = AutoloadSkyDict.LoadedSkies[key](Player);

			if (skyIsActive)
				SkyManager.Instance.Activate(key);

			else if (SkyManager.Instance[key].IsActive())
				SkyManager.Instance.Deactivate(key);
		}
	}
}