using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Backpacks;

public class GiantBag : BackpackItem
{
	public sealed class GiantBagLayer : PlayerDrawLayer
	{
		public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal(typeof(GiantBag), "GiantBag_Back", false);

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Backpacks);
		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			return; //DEBUG

			Texture2D texture = Texture.Value;
			Color color = drawInfo.colorArmorBody;
			Vector2 position = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2 + drawInfo.drawPlayer.width / 2), (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 2)) + drawInfo.drawPlayer.bodyPosition + drawInfo.rotationOrigin;
			Vector2 offset = Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;

			if (drawInfo.drawPlayer.gravDir == -1)
				offset.Y += 22;

			offset.X -= 8 * drawInfo.drawPlayer.direction;
			Rectangle source = texture.Frame(1, 4, 0, (int)(drawInfo.drawPlayer.bodyFrameCounter / 18) % 4);
			DrawData drawData = new(texture, position + offset, source, color, drawInfo.drawPlayer.bodyRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
			{
				shader = drawInfo.cBack
			};

			drawInfo.DrawDataCache.Add(drawData);
		}
	}

	protected override int SlotCap => 6;

	public override void Defaults()
	{
		Item.Size = new Vector2(34, 28);
		Item.value = Item.buyPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;
	}
}