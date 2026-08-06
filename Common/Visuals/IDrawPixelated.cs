using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals.RenderTargets;

namespace SpiritReforged.Common.Visuals;

public interface IDrawPixelated
{
	public sealed class PixelatedDrawSystem : ModSystem
	{
		public static readonly EasyTarget PixelTarget = new(new Vector2(0.5f));

		public override void Load()
		{
			TargetSetup.DrawIntoRendertargets += SetupPixelTarget;
			On_Main.DrawItems += DrawPixelTarget;
		}

		private static void SetupPixelTarget() //TODO: DEACTIVATE WHEN NOT IN USE
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			GraphicsDevice graphics = Main.graphics.GraphicsDevice;

			graphics.SetRenderTarget(PixelTarget.Value);
			graphics.Clear(Color.Transparent);
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

			graphics.BlendState = BlendState.AlphaBlend; //Required for prims

			foreach (Item item in Main.ActiveItems)
			{
				if (item.ModItem is IDrawPixelated iDrawPixelated)
					iDrawPixelated.DrawPixelated(spriteBatch);
			}

			foreach (Projectile projectile in Main.ActiveProjectiles)
			{
				if (projectile.ModProjectile is IDrawPixelated iDrawPixelated)
					iDrawPixelated.DrawPixelated(spriteBatch);
			}

			spriteBatch.End();
			graphics.SetRenderTarget(null);
		}

		private static void DrawPixelTarget(On_Main.orig_DrawItems orig, Main self)
		{
			if (PixelTarget.Value != null)
			{
				Vector2 offset = Main.screenPosition;

				offset.X %= 2;
				offset.Y %= 2;

				Main.spriteBatch.Draw(PixelTarget.Value, -offset, null, Color.White, 0, Vector2.Zero, 2, 0, 0);
			}

			orig(self);
		}
	}

	public void DrawPixelated(SpriteBatch spriteBatch);

	public static void PixelateDrawPosition(ref Vector2 position)
	{
		position += Main.screenPosition;
		position /= 2f;
		position.X -= (int)Main.screenPosition.X / 2;
		position.Y -= (int)Main.screenPosition.Y / 2;
	}
}