using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

internal class GreatshieldLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccBack);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Player plr = drawInfo.drawPlayer;

		if (plr.dead || plr.HeldItem.ModItem is not GreatshieldItem shield)
			return;

		const int JumpFrame = 5;

		Texture2D tex = GreatshieldItem.ShieldToHeldTexture[plr.HeldItem.type].Value;
		Color color = Lighting.GetColor(plr.Center.ToTileCoordinates());
		int currentFrame = plr.bodyFrame.Y / plr.bodyFrame.Height;
		Vector2 headOffset = Main.OffsetsPlayerOffhand[currentFrame]; // "Animates" the shield with position offsets
		bool right = plr.direction == 1;
		float xOffset = right ? 8 : 0; // Hardcoded specific offset because it's not centered properly when flipped
		float rotation = plr.AngleTo(PlayerMouseHandler.GetMouse(plr.whoAmI));
		float functionalRotation = rotation; // Rotation used for the aim direction instead of sprite rotation
		GreatshieldPlayer shieldPlayer = plr.GetModPlayer<GreatshieldPlayer>();

		if (plr.direction == -1)
			rotation += MathHelper.Pi;

		// This block handles actually animating the shield, w/ tweakable parameters - "thrust" animation
		if (plr.ItemAnimationActive && shieldPlayer.parryTime <= 0)
		{
			rotation = GetShieldAnimationData(plr, rotation, out float factor);

			int sign = Math.Sign(xOffset);
			if (sign == 0)
				sign = 1;

			xOffset += factor * 10 * sign;
		}

		// This factor & the following if handles the "guard" effect
		float guardFactor = 0;

		if (shieldPlayer.parryTime > 0)
			guardFactor = shieldPlayer.AnimationFactor;

		var basicOffset = new Vector2(0, 28) + new Vector2(12 + xOffset, 0).RotatedBy(functionalRotation) * new Vector2(1, 0.8f);

		if (currentFrame == JumpFrame)
			basicOffset.Y += 8;

		if (shieldPlayer.parryAnim > 0)
			basicOffset.Y -= 8 * shieldPlayer.AnimationFactor;

		Vector2 frame = tex.Size();
		int yOffset = plr.mount.Active ? plr.mount._data.yOffset : 0;
		Vector2 position = drawInfo.Position - Main.screenPosition + basicOffset + headOffset - frame / 2f + new Vector2(0, yOffset + plr.HeightMapOffset);
		SpriteEffects effect = !right ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		DrawData data = new(tex, position.Floor(), null, color, rotation, frame / 2f, 1f + guardFactor * 0.25f, effect, 0);
		shield.ModifyLayerDrawing(ref data, false);
		drawInfo.DrawDataCache.Add(data);

		if (guardFactor > 0)
		{
			DrawData guard = new(tex, position.Floor(), null, color * guardFactor * 0.5f, rotation, frame / 2f, 1f + (1 - guardFactor), effect, 0);
			shield.ModifyLayerDrawing(ref guard, true);
			drawInfo.DrawDataCache.Add(guard);
		}
	}

	internal static float GetShieldAnimationData(Player plr, float rotation, out float factor)
	{
		const float Anticipation = 0.4f;
		const float Push = 0.15f;

		const float AnticipationRotation = 0.2f;

		factor = 1 - plr.itemAnimation / (float)plr.itemAnimationMax;
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

		return rotation;
	}
}
