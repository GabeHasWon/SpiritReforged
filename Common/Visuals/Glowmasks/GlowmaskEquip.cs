using Terraria.DataStructures;

namespace SpiritReforged.Common.Visuals.Glowmasks;

internal class GlowmaskEquip : GlobalItem
{
	public static void AddGlowmaskBySlot(int slot, EquipType type, GlowmaskInfo info)
	{
		if (type == EquipType.Head)
		{
			GlowmaskHeadLayer.SlotToGlowmask.Add(slot, info);
		}
		else if (type == EquipType.Body)
		{
			GlowmaskTorsoLayer.SlotToGlowmask.Add(slot, info);
		}
		else if (type == EquipType.Legs)
		{
			GlowmaskLegsLayer.SlotToGlowmask.Add(slot, info);
		}
	}

	private class GlowmaskHeadLayer : PlayerDrawLayer
	{
		public static readonly Dictionary<int, GlowmaskInfo> SlotToGlowmask = [];

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			if (!SlotToGlowmask.TryGetValue(drawInfo.drawPlayer.head, out var value) || drawInfo.shadow != 0)
				return;

			Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X) + (drawInfo.drawPlayer.width - drawInfo.drawPlayer.bodyFrame.Width) / 2, (int)(drawInfo.Position.Y - Main.screenPosition.Y) + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4) + drawInfo.drawPlayer.headPosition + drawInfo.rotationOrigin;
			var color = value.GetDrawColor?.Invoke(null) ?? Color.White;

			var drawData = new DrawData(value.Glowmask.Value, pos, drawInfo.drawPlayer.bodyFrame, color, drawInfo.drawPlayer.headRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
			{ shader = drawInfo.cHead };

			drawInfo.DrawDataCache.Add(drawData);
		}
	}

	private class GlowmaskTorsoLayer : PlayerDrawLayer
	{
		public static readonly Dictionary<int, GlowmaskInfo> SlotToGlowmask = [];

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Torso);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			if (!SlotToGlowmask.TryGetValue(drawInfo.drawPlayer.body, out var value) || drawInfo.drawPlayer.invis || drawInfo.shadow != 0)
				return;

			var texture = value.Glowmask.Value;
			var color = value.GetDrawColor?.Invoke(null) ?? Color.White;
			Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2 + drawInfo.drawPlayer.width / 2), (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 2)) + drawInfo.drawPlayer.bodyPosition + drawInfo.rotationOrigin;
			Vector2 bobOff = Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;

			if (drawInfo.drawPlayer.gravDir == -1)
				bobOff.Y += 4;

			if (drawInfo.usesCompositeTorso)
			{
				var drawData = new DrawData(texture, pos + bobOff, drawInfo.compTorsoFrame, color, drawInfo.drawPlayer.bodyRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect)
				{ shader = drawInfo.cBody };

				drawInfo.DrawDataCache.Add(drawData);
			}
			else
			{
				var drawData = new DrawData(texture, pos + bobOff, drawInfo.drawPlayer.bodyFrame, color, drawInfo.drawPlayer.bodyRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
				{ shader = drawInfo.cBody };

				drawInfo.DrawDataCache.Add(drawData);
			}
		}
	}

	private class GlowmaskArmsLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			int slot = drawInfo.drawPlayer.body;

			if (!GlowmaskTorsoLayer.SlotToGlowmask.TryGetValue(slot, out var value) || drawInfo.drawPlayer.invis || drawInfo.shadow != 0)
				return;

			var texture = value.Glowmask.Value;
			var color = value.GetDrawColor?.Invoke(null) ?? Color.White;
			var bobOff = Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;

			if (drawInfo.drawPlayer.gravDir == -1)
				bobOff.Y += 4;

			if (drawInfo.usesCompositeTorso)
			{
				static Vector2 GetCompositeOffset_FrontArm(ref PlayerDrawSet drawinfo)
					=> new(-5 * ((!drawinfo.playerEffect.HasFlag(SpriteEffects.FlipHorizontally)) ? 1 : (-1)), 0f);

				Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width / 2 + drawInfo.drawPlayer.width / 2), (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 2)) + drawInfo.drawPlayer.bodyPosition + (drawInfo.drawPlayer.bodyFrame.Size() / 2);
				pos += GetCompositeOffset_FrontArm(ref drawInfo);

				Vector2 bodyVect = drawInfo.bodyVect + GetCompositeOffset_FrontArm(ref drawInfo);
				Vector2 shoulderPos = pos + drawInfo.frontShoulderOffset;
				if (drawInfo.compFrontArmFrame.X / drawInfo.compFrontArmFrame.Width >= 7)
					pos += new Vector2((!drawInfo.playerEffect.HasFlag(SpriteEffects.FlipHorizontally)) ? 1 : (-1), (!drawInfo.playerEffect.HasFlag(SpriteEffects.FlipVertically)) ? 1 : (-1));

				float rotation = drawInfo.drawPlayer.bodyRotation + drawInfo.compositeFrontArmRotation;
				var drawData = new DrawData(texture, pos + bobOff, drawInfo.compFrontArmFrame, color, rotation, bodyVect, 1f, drawInfo.playerEffect)
				{ shader = drawInfo.cBody };

				drawInfo.DrawDataCache.Add(drawData);

				if (!drawInfo.hideCompositeShoulders)
				{
					var drawData2 = new DrawData(texture, shoulderPos + bobOff, drawInfo.compFrontShoulderFrame, color, drawInfo.drawPlayer.bodyRotation, bodyVect, 1f, drawInfo.playerEffect)
					{ shader = drawInfo.cBody };

					drawInfo.DrawDataCache.Add(drawData2);
				}
			}
			else
			{
				Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - (drawInfo.drawPlayer.bodyFrame.Width / 2) + (drawInfo.drawPlayer.width / 2)), (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 2)) + drawInfo.drawPlayer.bodyPosition + drawInfo.rotationOrigin;
				var drawData = new DrawData(texture, pos + bobOff, drawInfo.drawPlayer.bodyFrame, drawInfo.bodyGlowColor, drawInfo.drawPlayer.bodyRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
				{ shader = drawInfo.cBody };

				drawInfo.DrawDataCache.Add(drawData);
			}
		}
	}

	private class GlowmaskLegsLayer : PlayerDrawLayer
	{
		public static readonly Dictionary<int, GlowmaskInfo> SlotToGlowmask = [];

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Leggings);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			if (!SlotToGlowmask.TryGetValue(drawInfo.drawPlayer.legs, out var value) || drawInfo.drawPlayer.invis || drawInfo.isSitting || drawInfo.shadow != 0)
				return;

			var texture = value.Glowmask.Value;
			var color = value.GetDrawColor?.Invoke(null) ?? Color.White;

			if (drawInfo.drawPlayer.shoe != 15 || drawInfo.drawPlayer.wearsRobe)
			{
				Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.legFrame.Width / 2 + drawInfo.drawPlayer.width / 2), (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.legFrame.Height + 4)) + drawInfo.drawPlayer.legPosition + drawInfo.rotationOrigin;
				var drawData = new DrawData(texture, pos, drawInfo.drawPlayer.legFrame, color, drawInfo.drawPlayer.legRotation, drawInfo.rotationOrigin, 1f, drawInfo.playerEffect, 0)
				{ shader = drawInfo.cLegs };

				drawInfo.DrawDataCache.Add(drawData);
			}
		}
	}
}