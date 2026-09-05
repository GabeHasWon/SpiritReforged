using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.RenderTargets;

namespace SpiritReforged.Common.Visuals;

public interface IDrawPixelated
{
	public sealed class PixelatedDrawLoader : ILoadable
	{
		public static readonly EasyTarget PixelTarget = new(new Vector2(0.5f));

		void ILoadable.Load(Mod mod)
		{
			TargetSetup.DrawIntoRendertargets += SetupPixelTarget;
			On_Main.DrawItems += DrawPixelTarget;
		}

		void ILoadable.Unload() { }

		private static void SetupPixelTarget()
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			GraphicsDevice graphics = Main.graphics.GraphicsDevice;

			List<IDrawPixelated> pixelQueue = []; //Setup queue
			foreach (Item item in Main.ActiveItems)
			{
				if (item.ModItem is IDrawPixelated iDrawPixelated)
					pixelQueue.Add(iDrawPixelated);
			}

			foreach (Projectile projectile in Main.ActiveProjectiles)
			{
				if (projectile.ModProjectile is IDrawPixelated iDrawPixelated)
					pixelQueue.Add(iDrawPixelated);
			}

			foreach (Particle.Particle particle in ParticleHandler.Particles)
			{
				if (particle is null || particle.TimeActive > particle.MaxTime)
					continue;

				if (particle is IDrawPixelated iDrawPixelated)
				{
					pixelQueue.Add(iDrawPixelated);
				}
			}

			if (pixelQueue.Count > 0) //Avoid restarting the spritebatch if there is nothing in queue
			{
				graphics.SetRenderTarget(PixelTarget.Value);
				graphics.Clear(Color.Transparent);
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

				graphics.BlendState = BlendState.AlphaBlend; //Required for prims

				foreach (IDrawPixelated iDrawPixelated in pixelQueue)
					iDrawPixelated.DrawPixelated(spriteBatch);

				spriteBatch.End();
				graphics.SetRenderTarget(null);
			}
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