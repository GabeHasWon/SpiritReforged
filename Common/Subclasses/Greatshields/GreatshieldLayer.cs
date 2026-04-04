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
		Vector2 headOffset = Main.OffsetsPlayerOffhand[currentFrame];
		bool right = plr.direction == 1;
		float xOffset = right ? 16 : -12;

		if (plr.ItemAnimationActive)
		{
			float factor = plr.itemAnimation / (float)plr.itemAnimationMax;

			xOffset += factor * 4 * Math.Sign(xOffset);
		}

		var basicOffset = new Vector2(xOffset - 16, -16);

		if (currentFrame == JumpFrame)
		{
			basicOffset.Y += 8;
		}

		Vector2 position = drawInfo.Position - Main.screenPosition + basicOffset + headOffset;
		SpriteEffects effect = !right ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		DrawData data = new(tex, position.Floor(), null, color, 0f, Vector2.Zero, 1f, effect, 0);

		shield.ModifyLayerDrawing(ref data);
		drawInfo.DrawDataCache.Add(data);
	}
}
