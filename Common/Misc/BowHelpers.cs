using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;

namespace SpiritReforged.Common.Misc;

public static class BowHelpers
{
	public static void FindAmmo(Player owner, int ammoID, out int projType, out int ammoDamage, out float ammoKB, out float ammoSpeed, int skipAmount = 0)
	{
		const int ammoInventoryStart = Main.InventoryAmmoSlotsStart;
		const int ammoInventoryEnd = Main.InventoryAmmoSlotsStart + Main.InventoryAmmoSlotsCount;

		projType = ProjectileID.None;
		ammoDamage = 0;
		ammoKB = 0;
		ammoSpeed = 0;

		int trySkip = 0;

		for (int i = ammoInventoryStart; i < ammoInventoryEnd; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projType = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoSpeed = selectedItem.shootSpeed;

				if (trySkip >= skipAmount)
					return;

				trySkip++;
			}
		}

		for (int i = 0; i < ammoInventoryStart; i++)
		{
			Item selectedItem = owner.inventory[i];
			if (selectedItem.ammo == ammoID && selectedItem.stack > 0)
			{
				projType = selectedItem.shoot;
				ammoDamage = selectedItem.damage;
				ammoKB = selectedItem.knockBack;
				ammoSpeed = selectedItem.shootSpeed;

				if (trySkip >= skipAmount)
					return;

				trySkip++;
			}
		}
	}

	public static void BowDraw(float curDrawbackProgress, float bounceProgress, float rotation, float stringLength, float maxDrawback, Vector2 drawPosition, Vector2 bowSize, Vector2 stringOrigin, Color stringColor, Action<Vector2, float> arrowDrawHook)
	{
		float stringHalfLength = stringLength / 2;
		const float stringScale = 2;

		float easedCharge = EaseFunction.EaseCircularOut.Ease(curDrawbackProgress);
		float curDrawback = easedCharge + (1 - EaseFunction.EaseOutElastic().Ease(bounceProgress)) * (1 - easedCharge);
		curDrawback *= maxDrawback;

		var pointTop = new Vector2(stringOrigin.X, stringOrigin.Y - stringHalfLength);
		var pointMiddle = new Vector2(stringOrigin.X - curDrawback, stringOrigin.Y);
		var pointBottom = new Vector2(stringOrigin.X, stringOrigin.Y + stringHalfLength);
		int splineIterations = 30;
		Vector2[] spline = Spline.CreateSpline([pointTop, pointMiddle, pointBottom], splineIterations);
		for (int i = 0; i < splineIterations; i++)
		{
			var pixelPos = spline[i];

			pixelPos = pixelPos.RotatedBy(rotation);
			pixelPos -= (bowSize / 2).RotatedBy(rotation);
			pixelPos += drawPosition - Main.screenPosition;

			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pixelPos, new Rectangle(0, 0, 1, 1), stringColor, rotation, Vector2.Zero, stringScale, SpriteEffects.None, 0);
		}

		arrowDrawHook(pointMiddle.RotatedBy(rotation), easedCharge);
	}
}