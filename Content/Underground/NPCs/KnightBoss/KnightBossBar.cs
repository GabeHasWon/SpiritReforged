using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.NPCs.KnightBoss;

public class KnightBossBar : ModBossBar
{
	private float _hitProgress;

	public static bool IsActive(NPC npc, out SealedKnight sealedKnight)
	{
		if (npc.ModNPC is SealedKnight sKnight && sKnight.shieldLife > 0)
		{
			sealedKnight = sKnight;
			return true;
		}

		sealedKnight = default;
		return false;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
	{
		int bossHeadIndex = npc.GetBossHeadTextureIndex(); //Set head texture
		if (bossHeadIndex == -1) //Failsafe
			bossHeadIndex = NPCID.Sets.BossHeadTextures[npc.type];

		var icon = TextureAssets.NpcHeadBoss[bossHeadIndex].Value;
		drawParams.IconTexture = icon;
		drawParams.IconFrame = icon.Frame();

		if (IsActive(npc, out _)) //Fade out when immune to damage
		{
			drawParams.BarColor = Color.Gray;
			drawParams.IconColor = Color.Gray;
			drawParams.ShowText = false;
		}

		return true;
	}

	public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
	{
		if (IsActive(npc, out var sealedKnight))
		{
			Texture2D bar = Main.Assets.Request<Texture2D>("Images/UI/UI_BossBar").Value;
			Rectangle barSource = new(32, 195, 456, 28);
			int realWidth = (int)(barSource.Width * (sealedKnight.shieldLife / 100f));

			spriteBatch.Draw(bar, drawParams.BarCenter - barSource.Size() / 2, barSource with { Width = realWidth }, Color.Cyan.Additive() * (1f + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 90f) * 0.5f), 0, Vector2.Zero, 1, 0, 0);

			Rectangle endBarSource = new(32, 253, 2, 28);
			spriteBatch.Draw(bar, drawParams.BarCenter - barSource.Size() / 2 + new Vector2(realWidth, 0), endBarSource, Color.Cyan.Additive(), 0, Vector2.Zero, 1, 0, 0);

			Texture2D shield = TextureAssets.Extra[ExtrasID.DefenseShield].Value;
			Rectangle shieldSource = shield.Frame();

			spriteBatch.Draw(shield, drawParams.BarCenter - new Vector2(drawParams.BarTexture.Width / 2 - 18, 0), shieldSource, Color.White, _hitProgress * 0.3f * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 10f), shieldSource.Size() / 2, 1 + _hitProgress * 0.2f, 0, 0);
			Utils.DrawBorderString(spriteBatch, (npc.ModNPC as SealedKnight).shieldLife.ToString(), drawParams.BarCenter, Main.MouseTextColorReal, 1, 0.5f, 0.5f);

			if (_hitProgress > 0)
				_hitProgress -= 0.1f;

			if (npc.justHit)
				_hitProgress = 1f;
		}
	}
}