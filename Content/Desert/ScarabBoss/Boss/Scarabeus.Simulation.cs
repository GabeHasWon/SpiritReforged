using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;

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
		Vector2 position = NPC.Center - Main.screenPosition;
		Vector2 scale = new(NPC.scale);

		for (int layer = Profile.Columns - 1; layer >= 0; layer--)
		{
			Vector2 origin = (effects == SpriteEffects.FlipHorizontally) ? new(NPC.frame.Width - Components[layer].Origin.X, Components[layer].Origin.Y) : Components[layer].Origin;
			float rotation = NPC.rotation;
			rotation += Math.Clamp(NPC.velocity.X, -3, 3) * Components[layer].PhysicsStrength;

			Rectangle frame = texture.Frame(Profile.Columns, Profile.Rows, layer, currentFrame.Y);
			Main.EntitySpriteDraw(texture, position + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation), frame, color, rotation, origin, scale, effects);

			if (layer == 5) //Body layer glowmask
			{
				Texture2D glowmask = Profile.GlowMask.Value;

				float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
				Main.EntitySpriteDraw(glowmask, position + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation), frame, NPC.DrawColor(Color.White), rotation, origin, scale, effects);

				DrawHelpers.DrawOutline(Main.spriteBatch, glowmask, NPC.Center - Main.screenPosition, default, (offset) =>
					Main.EntitySpriteDraw(glowmask, position + offset + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation), frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, rotation, origin, scale, effects));
			}
		}
	}
}