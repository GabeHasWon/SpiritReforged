using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon;

public interface IDrawHeld
{
	public sealed class CustomHeldLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			if (drawInfo.drawPlayer.HeldItem.ModItem is IDrawHeld iHeld)
				iHeld.DrawHeld(ref drawInfo);
		}
	}

	public void DrawHeld(ref PlayerDrawSet drawinfo);

	public static void DrawSwordHeld(ref PlayerDrawSet drawinfo, Texture2D texture, Rectangle? source = null)
	{
		source ??= texture.Frame();

		Vector2 bobOffset = Main.OffsetsPlayerHeadgear[drawinfo.drawPlayer.bodyFrame.Y / drawinfo.drawPlayer.bodyFrame.Height] * drawinfo.drawPlayer.gravDir;
		Vector2 center = drawinfo.drawPlayer.MountedCenter + bobOffset;
		Vector2 drawPos = new((int)(center.X - Main.screenPosition.X), (int)(center.Y + 6 * drawinfo.drawPlayer.gravDir - Main.screenPosition.Y + drawinfo.drawPlayer.gfxOffY));

		float rotation = -0.15f * drawinfo.drawPlayer.direction + drawinfo.drawPlayer.fullRotation + MathHelper.Pi;
		Color color = Lighting.GetColor((int)drawinfo.drawPlayer.Center.X / 16, (int)drawinfo.drawPlayer.Center.Y / 16);

		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPos, source, color, rotation, source.Value.Size() / 2, 1, drawinfo.playerEffect, 0));
	}
}