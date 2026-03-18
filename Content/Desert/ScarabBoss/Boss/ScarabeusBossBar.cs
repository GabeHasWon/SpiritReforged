using SpiritReforged.Common.Particle;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class ScarabeusBossBar : ModBossBar
{
	public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
	{
		var icon = TextureAssets.NpcHeadBoss[npc.GetBossHeadTextureIndex()].Value;
		drawParams.IconTexture = icon;
		drawParams.IconFrame = icon.Frame();

		if (npc.dontTakeDamage) //Fade out when immune to damage
		{
			drawParams.BarColor = Color.Gray;
			drawParams.IconColor = Color.Gray;
			drawParams.ShowText = false;
		}

		return true;
	}

	public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
	{
		if (npc.ModNPC is Scarabeus scarabeus && scarabeus.CurrentState == Scarabeus.AIState.PhaseTransitionAnim && scarabeus.currentFrame.X == 5)
		{
			Texture2D texture = ParticleHandler.GetTexture(ParticleHandler.TypeOf<FireSploshion>());
			Rectangle source = texture.Frame(2, 7, 0, (int)(scarabeus.Counter / 30f * 6));

			spriteBatch.Draw(texture, drawParams.BarCenter - new Vector2(drawParams.BarTexture.Width / 2 - 14, 0), source, Color.White, 0, source.Size() / 2, 1, 0, 0);
		}
	}
}