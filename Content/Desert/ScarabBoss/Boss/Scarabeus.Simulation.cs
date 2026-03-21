using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using Terraria;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	private readonly record struct SimulatedComponent(Vector2 Origin, float PhysicsStrength);

	private static readonly SimulatedComponent[] Components = new SimulatedComponent[]
	{
		new(new(114, 42), 0),
		new(new(122, 56), 0),
		new(new(84, 102), 0.1f),
		new(new(102, 100), 0.05f),
		new(new(120, 94), 0.025f),
		new(Vector2.Zero, 0),
		new(new(72, 100), 0.1f),
		new(new(90, 100), 0.05f),
		new(new(100, 102), 0.025f),
		new(new(90, 56), 0),
		new(new(72, 50), 0)
	};

	private void DrawSimulated(Color color, SpriteEffects effects)
	{
		Texture2D texture = Profile.Texture.Value;
		Vector2 basePosition = NPC.Center - Main.screenPosition;
		Vector2 scale = new(NPC.scale);

		for (int layer = Profile.Columns - 1; layer >= 0; layer--)
		{
			Vector2 origin = (effects == SpriteEffects.FlipHorizontally) ? new(NPC.frame.Width - Components[layer].Origin.X, Components[layer].Origin.Y) : Components[layer].Origin;
			Vector2 position = basePosition + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation);
			float rotation = NPC.rotation;
			rotation += Math.Clamp(NPC.velocity.X * Components[layer].PhysicsStrength, -3, 3);

			Rectangle frame = texture.Frame(Profile.Columns, Profile.Rows, layer, currentFrame.Y);
			Color drawColor = color;

			//Wing special drawing
			if (layer is 1 or 10)
			{
				int WingFrame(int offset = 0) => (int)(wingFrameCounter + offset) % Profile.Rows;

				//draw a subtle afterimage of the previous frame of the wing anim- unsure of this but i thiiink it looks better for selling the super fast wing motion?
				frame = texture.Frame(Profile.Columns, Profile.Rows, layer, WingFrame(3));
				Main.EntitySpriteDraw(texture, position, frame, drawColor * 0.125f, rotation, origin, scale, effects);

				frame = texture.Frame(Profile.Columns, Profile.Rows, layer, WingFrame(0));
				//Wing afterimage trail
				for (int i = NPCID.Sets.TrailCacheLength[Type] - 1; i >= 0; i--)
				{
					float progress = 1 - i / (float)NPCID.Sets.TrailCacheLength[Type];
					progress = EaseFunction.EaseCircularIn.Ease(progress);
					Vector2 trailPosition = NPC.oldPos[i] + NPC.Size / 2 - Main.screenPosition + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation);

					Main.EntitySpriteDraw(texture, trailPosition, frame, drawColor * 0.75f * progress, rotation, origin, scale, effects);
				}

				Main.EntitySpriteDraw(texture, position, frame, drawColor * 0.75f, rotation, origin, scale, effects);
				continue;
			}

			Main.EntitySpriteDraw(texture, position, frame, drawColor, rotation, origin, scale, effects);

			if (layer == 5) //Body layer glowmask
			{
				Texture2D glowmask = Profile.GlowMask.Value;

				float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
				Main.EntitySpriteDraw(glowmask, basePosition + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation), frame, NPC.DrawColor(Color.White), rotation, origin, scale, effects);

				DrawHelpers.DrawOutline(Main.spriteBatch, glowmask, NPC.Center - Main.screenPosition, default, (offset) =>
					Main.EntitySpriteDraw(glowmask, position, frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, rotation, origin, scale, effects));
			}
		}
	}
}