using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ziggurat.Scarab;

internal abstract class ScarabLayerBase : PlayerDrawLayer
{
	public const int NumFramesY = 8;
	public const int FrameDuration = 8;

	private static readonly Asset<Texture2D> ScarabShieldTexture = DrawHelpers.RequestLocal<ScarabLayerBase>("GildedScarab_Player", false);

	protected abstract bool CanDraw(int visualCounter);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		if (drawInfo.shadow != 0f)
			return;

		var player = drawInfo.drawPlayer;
		var mPlayer = player.GetModPlayer<GildedScarabPlayer>();

		if (mPlayer.opacity > 0)
		{
			if (!CanDraw((int)mPlayer.visualCounter))
				return;

			var texture = ScarabShieldTexture.Value;
			var source = texture.Frame(1, NumFramesY, 0, (int)mPlayer.visualCounter, sizeOffsetY: -2);
			float rotation = (float)Math.Sin(Main.timeForVisualEffects / 30f) * .2f;
			var position = player.MountedCenter - Main.screenPosition + new Vector2(-(float)Math.Sin(mPlayer.visualCounter / NumFramesY * MathHelper.TwoPi) * 22f, rotation * 20 + player.gfxOffY);
			var color = Lighting.GetColor(player.Center.ToTileCoordinates()) * mPlayer.opacity;

			var drawData = new DrawData(texture, position, source, color, rotation, source.Size() / 2, 1, SpriteEffects.None, 0);
			drawInfo.DrawDataCache.Add(drawData);
		}
	}
}

internal class ScarabFrontLayer : ScarabLayerBase
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ProjectileOverArm);
	protected override bool CanDraw(int visualCounter) => visualCounter is 0 or 1 or 2 or 7;
}

internal class ScarabBackLayer : ScarabLayerBase
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ForbiddenSetRing);
	protected override bool CanDraw(int visualCounter) => visualCounter is 3 or 4 or 5 or 6;
}