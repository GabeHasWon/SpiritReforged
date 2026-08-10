using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.Subclasses.Wrenches;

public interface IWrenchGlobal
{
	public int Duration { get; set; }

	public static void DrawDurationBar(Projectile projectile, float progress)
	{
		Texture2D bar = Main.Assets.Request<Texture2D>("Images/HealthBar1").Value;
		Texture2D barBack = Main.Assets.Request<Texture2D>("Images/HealthBar2").Value;
		Texture2D grid = AssetLoader.LoadedTextures["GridPattern"].Value;
		//Texture2D cog = TextureAssets.Item[ItemID.Cog].Value;

		float grabProgress = Main.LocalPlayer.GetModPlayer<WrenchPlayer>().scrapGrabTimer / (float)WrenchPlayer.SCRAP_GRAB_MAX;
		float rotation = grabProgress * 0.2f;

		//float cogRotation = (float)(Main.timeForVisualEffects / 15f) + rotation;
		//float cogScale = 0.7f * Math.Min(progress * 10, 1);

		Vector2 position = projectile.Bottom - Main.screenPosition + new Vector2(-(bar.Width / 2), 6 - grabProgress * 4);

		/*DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
			Main.EntitySpriteDraw(TextureColorCache.ColorSolid(cog, Color.White), position + offset * cogScale, null, Color.Yellow.Additive(100), cogRotation, cog.Size() / 2, cogScale, 0));

		Main.EntitySpriteDraw(cog, position, null, Color.Gray, cogRotation, cog.Size() / 2, cogScale, 0);*/
		Main.EntitySpriteDraw(barBack, position, null, Color.White, rotation, Vector2.Zero, 1, 0);

		int width = (int)Math.Round(bar.Width * progress / 2f) * 2;
		Main.EntitySpriteDraw(bar, position, new Rectangle(0, 0, width, bar.Height), Color.Lerp(Color.Red, Color.Orange, progress).Additive(100), rotation, Vector2.Zero, 1, 0);

		int scroll = (int)Math.Round(Main.timeForVisualEffects * 0.3f / 2f) * 2;
		Main.EntitySpriteDraw(grid, position, new Rectangle(scroll % (grid.Width - 32), scroll % (grid.Height - 32), width, bar.Height), Color.Lerp(Color.OrangeRed, Color.Yellow, progress).Additive() * 0.4f, rotation, Vector2.Zero, 1, 0);
	}
}