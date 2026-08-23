using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;
public class MagazinePlayer : ModPlayer
{
	public static bool HoldingMagazineWeapon => Main.LocalPlayer.HeldItem.TryGetGlobalItem<MagazineGlobalItem>(out var globalItem) && globalItem.Active;

	public override void Load() => CustomCursor.DrawCustomCursor += DrawAmmo;

	private static void DrawAmmo(bool thick)
	{
		if (!Main.LocalPlayer.mouseInterface && HoldingMagazineWeapon && Main.LocalPlayer.HeldItem.TryGetGlobalItem<MagazineGlobalItem>(out var globalItem))
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D texture = ModContent.Request<Texture2D>("SpiritReforged/Common/ItemCommon/MagazineSystem/MagazineBar").Value;

			Vector2 position = Main.MouseScreen + new Vector2(32, 16);

			float factor = 1 - globalItem.GetCurrentMagazine().AmmoUsed / (float)globalItem.GetMagazineData()._magazineSize;

			var source = new Rectangle(0, 0, (int)(factor * texture.Width), texture.Height);
			var target = new Rectangle((int)position.X,
				(int)position.Y, (int)(factor * texture.Width), texture.Height);

			if (Main.LocalPlayer.TryGetModPlayer(out MagazinePlayer modPlayer))
			{
				spriteBatch.Draw(texture, position + new Vector2(0, 10), null, Color.White * 0.25f, 0f, texture.Size() / 2f, 1f, 0f, 0f);

				spriteBatch.Draw(texture, target, source, Color.White, 0f, new Vector2(texture.Width / 2, 0), 0f, 0f);
			}
		}
	}
}
