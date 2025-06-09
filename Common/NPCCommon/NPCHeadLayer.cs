using SpiritReforged.Common.MapCommon;
using System.Reflection;
using Terraria.Graphics.Renderers;
using Terraria.Map;

namespace SpiritReforged.Common.NPCCommon;

internal class NPCHeadLayer : ModMapLayer
{
	/// <summary> The types of outlier NPCs that use <see cref="AutoloadHead"/>. </summary>
	private static readonly HashSet<int> Types = [];
	private NPCHeadRenderer _renderer;

	public sealed override void SetupContent()
	{
		foreach (var npc in Mod.GetContent<ModNPC>())
		{
			if (npc.GetType().GetCustomAttribute<AutoloadHead>() is not null && !npc.NPC.townNPC)
				Types.Add(npc.Type);
		}

		Main.ContentThatNeedsRenderTargets.Add(_renderer = new(TextureAssets.NpcHead));
	}

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
			c.mapScale = 1f;

		var position = MapUtils.TranslateToMap(npc.Center / 16f, c);
		if (c.clippingRect.HasValue && !c.clippingRect.Value.Contains(position.ToPoint()))
			return;

		float drawScale = c.drawScale * scale;
		var effects = (npc.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		_renderer.DrawWithOutlines(npc, headId, position, Color.White, 0, drawScale, effects);
		//if (context.Draw(target, npc.Center / 16f, Color.White, new SpriteFrame(1, 1, 0, 0), scale, scale, Alignment.Center).IsMouseOver && Main.mapFullscreen)
		//	text = npc.FullName; //MapOverlayDrawContext can't use SpriteEffects??

		if (Main.mapFullscreen) //Hover effects
		{
			var scaledSize = (headTexture.Size() * drawScale).ToPoint();
			if (new Rectangle((int)position.X - scaledSize.X / 2, (int)position.Y - scaledSize.Y / 2, scaledSize.X, scaledSize.Y).Contains(new Point(Main.mouseX, Main.mouseY)))
				text = npc.FullName;
		}
	}
}