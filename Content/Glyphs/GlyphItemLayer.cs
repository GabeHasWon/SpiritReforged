using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.UI;
using static SpiritReforged.Content.Glyphs.GlyphItem;

namespace SpiritReforged.Content.Glyphs;

public class GlyphItemLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Player player = drawInfo.drawPlayer;

		if (player.JustDroppedAnItem || !player.HeldItem.HasGlyph())
			return;

		if (player.heldProj >= 0 && drawInfo.shadow == 0f && drawInfo.heldProjOverHand)
			drawInfo.projectileDrawPosition = drawInfo.DrawDataCache.Count;

		player.HeldItem.GetGlobalItem<GlyphGlobalItem>().HasGlyph(out var GlyphItem);

		Item heldItem = drawInfo.heldItem;
		int type = heldItem.type;
		Main.instance.LoadItem(type);
		float itemScale = player.GetAdjustedItemScale(heldItem);
		Texture2D tex = TextureColorCache.ColorSolid(TextureAssets.Item[player.HeldItem.type].Value, Color.White);
		var position = new Vector2((int)(drawInfo.ItemLocation.X - Main.screenPosition.X), (int)(drawInfo.ItemLocation.Y - Main.screenPosition.Y));
		Rectangle frame = player.GetItemDrawFrame(type);
		drawInfo.itemColor = Lighting.GetColor((int)(drawInfo.Position.X + player.width * 0.5) / 16,
			(int)((drawInfo.Position.Y + player.height * 0.5) / 16.0));

		if (player.shroomiteStealth && heldItem.CountsAsClass(DamageClass.Ranged))
		{
			float stealth = MathF.Max(player.stealth, 0.03f);
			float adj = (1f + stealth * 10f) / 11f;

			drawInfo.itemColor = new Color((byte)(drawInfo.itemColor.R * stealth), (byte)(drawInfo.itemColor.G * stealth), (byte)(drawInfo.itemColor.B * adj),
				(byte)(drawInfo.itemColor.A * stealth));
		}

		if (player.setVortex && heldItem.CountsAsClass(DamageClass.Ranged))
		{
			float stealth = MathF.Max(player.stealth, 0.03f);
			drawInfo.itemColor = drawInfo.itemColor.MultiplyRGBA(new Color(Vector4.Lerp(Vector4.One, new Vector4(0f, 0.12f, 0.16f, 0f), 1f - stealth)));
		}

		bool usingItem = player.itemAnimation > 0 && heldItem.useStyle != ItemUseStyleID.None;
		bool canHoldStyle = heldItem.holdStyle != 0 && !player.pulley;

		if (!player.CanVisuallyHoldItem(heldItem))
			canHoldStyle = false;

		if (drawInfo.shadow != 0f || player.frozen || !(usingItem || canHoldStyle) || type <= 0 || player.dead || heldItem.noUseGraphic
			|| player.wet && heldItem.noWet && !ItemID.Sets.WaterTorches[type]
			|| player.happyFunTorchTime && player.inventory[player.selectedItem].createTile == TileID.Torches
			&& player.itemAnimation == 0)
			return;

		Vector2 basePosition = Vector2.Zero;

		var origin = new Vector2(frame.Width * 0.5f - frame.Width * 0.5f * player.direction, frame.Height);

		if (heldItem.useStyle == ItemUseStyleID.DrinkLiquid && player.itemAnimation > 0)
		{
			var vector2 = new Vector2(0.5f, 0.4f);
			origin = frame.Size() * vector2;
		}

		if (player.gravDir == -1f)
			origin.Y = frame.Height - origin.Y;

		origin += basePosition;
		float itemRotation = player.itemRotation;

		if (heldItem.useStyle == ItemUseStyleID.GolfPlay)
		{
			ref float x = ref position.X;
			float num6 = x;
			_ = player.direction;
			x = num6 - 0f;
			itemRotation -= (float)Math.PI / 2f * player.direction;
			origin.Y = 2f;
			origin.X += 2 * player.direction;
		}

		ItemSlot.GetItemLight(ref drawInfo.itemColor, heldItem);
		Color drawColor = Color.White;
		DrawData item;

		if (heldItem.useStyle == ItemUseStyleID.Shoot)
		{
			if (Item.staff[type])
			{
				float rotation = player.itemRotation + MathHelper.PiOver2 / 2f * player.direction;
				float xOffset = 0f;
				var holdOrigin = new Vector2(0f, frame.Height);

				if (player.gravDir == -1f)
				{
					if (player.direction == -1)
					{
						rotation += 1.57f;
						holdOrigin = new Vector2(frame.Width, 0f);
						xOffset -= frame.Width;
					}
					else
					{
						rotation -= 1.57f;
						holdOrigin = Vector2.Zero;
					}
				}
				else if (player.direction == -1)
				{
					holdOrigin = new Vector2(frame.Width, frame.Height);
					xOffset -= frame.Width;
				}

				ItemLoader.HoldoutOrigin(player, ref holdOrigin);

				item = new DrawData(tex, new Vector2((int)(drawInfo.ItemLocation.X - Main.screenPosition.X + holdOrigin.X + xOffset),
					(int)(drawInfo.ItemLocation.Y - Main.screenPosition.Y)), frame, drawColor, rotation,
					holdOrigin, itemScale, drawInfo.itemEffect);

				GlyphItem.DrawHeldItem(ref drawInfo, item);
				return;
			}

			var offset = new Vector2(0, frame.Height / 2);
			Vector2 drawPos = Main.DrawPlayerItemPos(player.gravDir, type);
			int baseX = (int)drawPos.X;
			offset.Y = drawPos.Y;
			var drawOrigin = new Vector2(-baseX, frame.Height / 2);

			if (player.direction == -1)
				drawOrigin = new Vector2(frame.Width + baseX, frame.Height / 2);

			var pos = new Vector2((int)(drawInfo.ItemLocation.X - Main.screenPosition.X + offset.X), (int)(drawInfo.ItemLocation.Y - Main.screenPosition.Y + offset.Y));
			item = new DrawData(tex, pos, null, drawColor, player.itemRotation, drawOrigin, itemScale, drawInfo.itemEffect);

			GlyphItem.DrawHeldItem(ref drawInfo, item);
			return;
		}

		if (player.gravDir == -1f)
		{
			item = new DrawData(tex, position, frame, drawColor, itemRotation, origin, itemScale, drawInfo.itemEffect);
			GlyphItem.DrawHeldItem(ref drawInfo, item);
			return;
		}

		item = new DrawData(tex, position, frame, drawColor, itemRotation, origin, itemScale, drawInfo.itemEffect);

		GlyphItem.DrawHeldItem(ref drawInfo, item);
	}
}