using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal class GreatshieldLayer : PlayerDrawLayer
{
	internal class HideShieldPlayer : ModPlayer
	{
		public override void HideDrawLayers(PlayerDrawSet drawInfo)
		{
			if (drawInfo.drawPlayer.HeldItem.ModItem is GreatshieldItem)
				PlayerDrawLayers.Shield.Hide();
		}
	}

	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccBack);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Player plr = drawInfo.drawPlayer;

		if (plr.HeldItem.ModItem is not GreatshieldItem shield)
			return;

		const int JumpFrame = 5;

		Texture2D tex = GreatshieldItem.ShieldToHeldTexture[plr.HeldItem.type].Value;
		Color color = Lighting.GetColor(plr.Center.ToTileCoordinates());
		int currentFrame = plr.bodyFrame.Y / plr.bodyFrame.Height;
		Vector2 headOffset = Main.OffsetsPlayerOffhand[currentFrame]; // "Animates" the shield with position offsets
		bool right = plr.direction == 1;
		float xOffset = right ? 8 : 0; // Hardcoded specific offset because it's not centered properly when flipped
		float rotation = plr.AngleTo(Main.MouseWorld);
		float functionalRotation = rotation; // Rotation used for the aim direction instead of sprite rotation

		if (plr.direction == -1)
			rotation += MathHelper.Pi;

		// This block handles actually animating the shield, w/ tweakable parameters
		if (plr.ItemAnimationActive)
		{
			const float Anticipation = 0.4f;
			const float Push = 0.15f;

			const float AnticipationRotation = 0.2f;

			float factor = 1 - plr.itemAnimation / (float)plr.itemAnimationMax;

			if (factor < Anticipation)
			{
				factor = MathHelper.Lerp(0, -0.4f, factor / Anticipation);
				rotation += MathHelper.Lerp(0, AnticipationRotation * -plr.direction, factor / Anticipation);
			}
			else if (factor < Anticipation + Push)
			{
				factor = MathHelper.Lerp(-0.1f, 1f, (factor - Anticipation) / Push);
				rotation += MathHelper.Lerp(AnticipationRotation * -plr.direction, 0, (factor - Anticipation) / Push);
			}
			else
				factor = MathHelper.Lerp(1, 0, (factor - (Push + Anticipation)) / (1 - (Push + Anticipation)));

			int sign = Math.Sign(xOffset);

			if (sign == 0)
				sign = 1;

			xOffset += factor * 12 * sign;
		}

		var basicOffset = new Vector2(0, 28) + new Vector2(12 + xOffset, 0).RotatedBy(functionalRotation);

		if (currentFrame == JumpFrame)
			basicOffset.Y += 8;

		Vector2 frame = tex.Size();
		Vector2 position = drawInfo.Position - Main.screenPosition + basicOffset + headOffset - frame / 2f;
		SpriteEffects effect = !right ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		DrawData data = new(tex, position.Floor(), null, color, rotation, frame / 2f, 1f, effect, 0);

		shield.ModifyLayerDrawing(ref data);
		drawInfo.DrawDataCache.Add(data);
	}
}
