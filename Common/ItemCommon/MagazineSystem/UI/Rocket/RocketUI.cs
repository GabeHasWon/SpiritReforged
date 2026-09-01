using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem.UI.Rocket;
public class RocketUI
{
	public static readonly Asset<Texture2D> RocketTexture = DrawHelpers.RequestLocal<RocketUI>("RocketUI", false);
	public static readonly Asset<Texture2D> RocketOutline = DrawHelpers.RequestLocal<RocketUI>("RocketUI_Outline", false);

	public class RocketUIAmmo(Vector2 position, Vector2 velocity, int timeLeft, bool empowered = false) : MagazineUIAmmo(position, velocity, timeLeft, empowered)
	{
		public override void DoUpdate()
		{
			_velocity *= 0.975f;
			_velocity = _velocity.RotatedBy(-0.05f);

			offset += _velocity;

			rotation = _velocity.ToRotation();
		}

		public override void Draw(SpriteBatch sb)
		{
			Texture2D texture = RocketTexture.Value;
			Texture2D outlineTexture = RocketOutline.Value;

			if (Progress > 0.5f)
			{
				float lerp = (Progress - 0.5f) / 0.5f;
				sb.Draw(outlineTexture, offset - Main.screenPosition, null, Color.Lerp(_empowered ? Color.Yellow : Color.Orange, Main.MouseBorderColor, 1 - lerp) * lerp, rotation, outlineTexture.Size() / 2f, _scale, 0f, 0f);
			}

			sb.Draw(texture, offset - Main.screenPosition, null, Main.mouseColor * Progress, rotation, texture.Size() / 2f, _scale, 0f, 0f);
		}
	}

	public static void DrawUI(SpriteBatch sb, int _count, int uiSlotMoveTime, int maxMoveTime, int empoweredCount, int empoweredFlashTimer, int maxEmpoweredFlashTimer)
	{
		if (Main.LocalPlayer.TryGetModPlayer(out MagazinePlayer modPlayer) && MagazinePlayer.TryGetMagazineWeapon(Main.LocalPlayer, out var magazineWeapon))
		{
			Texture2D texture = RocketTexture.Value;
			Texture2D outlineTexture = RocketOutline.Value;
			Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
			Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

			int magazineSize = modPlayer.GetMagazineSize();
			var magazine = magazineWeapon.GetCurrentMagazine();

			const int offsetSize = 12;
			const int maxVisible = 5;

			for (int i = 0; i < _count; i++)
			{
				float fadeOut = (i + 1) / (float)_count;

				if (_count >= maxVisible)
				{
					if (i < _count - maxVisible) // anything more than five just gets turned invisible
						fadeOut = 0f;
					else
					{
						fadeOut = (i - (_count - maxVisible)) / (float)maxVisible;
					}
				}

				Vector2 position = Main.MouseScreen + new Vector2(32, magazineSize * offsetSize);

				float moveShellUp = MathHelper.Lerp((magazine.AmmoUsed - 1) * offsetSize, magazine.AmmoUsed * offsetSize, uiSlotMoveTime > 0 ? EaseBuilder.EaseCircularOut.Ease(1 - uiSlotMoveTime / (float)maxMoveTime) : 1f);

				if (magazineWeapon.Reloading && uiSlotMoveTime <= 0)
					moveShellUp = magazineWeapon.GetCurrentMagazine().AmmoUsed * offsetSize;

				Vector2 offset = new Vector2(0, -offsetSize * i - moveShellUp);

				if (i < empoweredCount)
				{
					float sine = (float)Math.Sin(Main.timeForVisualEffects / 20f);

					Vector2 shake = Vector2.Zero;
					float rotation = sine * 0.2f;
					float scale = 1f;
					if (sine > 0)
					{
						scale += sine * 0.1f;
						shake += Main.rand.NextVector2CircularEdge(sine, sine);
					}

					if (empoweredFlashTimer > 0)
					{
						float lerp = empoweredFlashTimer / (float)maxEmpoweredFlashTimer;

						Vector2 starScale = new Vector2(MathHelper.Lerp(0.1f, 0.3f, 1f - lerp), 0.1f);

						sb.Draw(starTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1.2f, 0f, 0f);
						sb.Draw(starTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1f, 0f, 0f);
					}

					sb.Draw(outlineTexture, position + offset + shake, null, Color.Yellow * fadeOut, rotation, outlineTexture.Size() / 2f, scale, 0f, 0f);
					sb.Draw(texture, position + offset + shake, null, Main.mouseColor * fadeOut, rotation, texture.Size() / 2f, scale, 0f, 0f);

					sb.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * 0.15f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
					sb.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * 0.1f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);

					if (empoweredFlashTimer > 0)
					{
						float lerp = empoweredFlashTimer / (float)maxEmpoweredFlashTimer;

						sb.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp * 0.5f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
						sb.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp * 0.35f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);
					}

					continue;
				}

				sb.Draw(outlineTexture, position + offset, null, Main.MouseBorderColor * fadeOut, 0f, outlineTexture.Size() / 2f, 1f, 0f, 0f);
				sb.Draw(texture, position + offset, null, Main.mouseColor * fadeOut, 0f, texture.Size() / 2f, 1f, 0f, 0f);
			}
		}
	}

	public static MagazineUIAmmo OnEject(int empoweredCount)
	{
		Vector2 position = Main.MouseWorld + new Vector2(32, 12) / Main.GameViewMatrix.Zoom;

		return new RocketUIAmmo(position, Vector2.UnitX.RotatedByRandom(0.35f) * Main.rand.NextFloat(6f, 12f), 60, empoweredCount > 0);
	}
}

