using SpiritReforged.Common.MapCommon;
using Terraria.Graphics.Renderers;
using Terraria.Map;

namespace SpiritReforged.Common.NPCCommon;

internal class NPCHeadLayer : ModMapLayer
{
	// This class solely fixes a weird issue where Main.mapMinimapScale jitters.
	internal class LastScale : ModPlayer
	{
		internal static float LastMinimapScale = 0;

		public override void PostUpdate() => LastMinimapScale = Main.mapMinimapScale;
	}

	/// <summary> The types of outlier NPCs that use <see cref="AutoloadHead"/>. </summary>
	internal static readonly HashSet<int> Types = [];

	private NPCHeadRenderer _renderer;

	public sealed override void SetupContent() => Main.ContentThatNeedsRenderTargets.Add(_renderer = new(TextureAssets.NpcHead));

	public override Position GetDefaultPosition() => new Before(IMapLayer.Pylons);

	public override void Draw(ref MapOverlayDrawContext context, ref string text)
	{
		foreach (var npc in Main.ActiveNPCs)
		{
			if (!Types.Contains(npc.type))
				continue;

			DrawHead(ref text, npc);
		}
	}

	private void DrawHead(ref string text, NPC npc)
	{
		const float scale = 1f;

		int headId = TownNPCProfiles.GetHeadIndexSafe(npc);
		if (headId == -1)
			return;

		var headTexture = TextureAssets.NpcHead[headId];

		MapUtils.PublicOverlayContext c = MapUtils.Context;

		if (!Main.mapFullscreen)
			c.mapScale = LastScale.LastMinimapScale;

		var position = MapUtils.TranslateToMap(npc.Center / 16f, c);

		if (c.clippingRect.HasValue && !c.clippingRect.Value.Contains(position.ToPoint()))
			return;

		float drawScale = c.drawScale * scale;
		var effects = (npc.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		_renderer.DrawWithOutlines(npc, headId, position, Color.White, 0, drawScale, effects);

		if (Main.mapFullscreen) //Hover effects
		{
			var scaledSize = (headTexture.Size() * drawScale).ToPoint();
			if (new Rectangle((int)position.X - scaledSize.X / 2, (int)position.Y - scaledSize.Y / 2, scaledSize.X, scaledSize.Y).Contains(new Point(Main.mouseX, Main.mouseY)))
				text = npc.FullName;
		}
	}
}