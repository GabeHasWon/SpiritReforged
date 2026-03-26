using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using Terraria;
using static SpiritReforged.Common.TileCommon.DrawOrderAttribute;

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

	private void DrawSimulated(SpriteBatch spriteBatch, Color color, SpriteEffects effects)
	{
		Texture2D texture = Profile.Texture.Value;
		Vector2 basePosition = NPC.Center - Main.screenPosition;
		Vector2 scale = new(NPC.scale);

		Effect sheenShader = GetShader(texture, Profile.SheenMask.Value, NPC.frame);

		for (int layer = Profile.Columns - 1; layer >= 0; layer--)
		{
			//Toggle shader after we drew the back wings (Columns - 2) and the front wings (1)
			if (layer == Profile.Columns - 2 || layer == 0)
				FlipShadersOnOff(spriteBatch, sheenShader, true);

			//Toggle shader OFF for front wings
			if (layer == 1)
				FlipShadersOnOff(spriteBatch, null, false);

			DrawSimulatedLayer(layer, texture, basePosition, scale, color, effects, sheenShader);

			//Draw the body layer glowmask
			if (layer == 5 && Profile.GlowMask != null)
			{
				FlipShadersOnOff(spriteBatch, null, false);
				DrawSimulatedLayer(layer, Profile.GlowMask.Value, basePosition, scale, color, effects, sheenShader, true);
				FlipShadersOnOff(spriteBatch, sheenShader, true);
			}
		}

		FlipShadersOnOff(spriteBatch, null, false);
	}

	private void DrawSimulatedLayer(int layer, Texture2D texture, Vector2 basePosition, Vector2 scale, Color drawColor, SpriteEffects effects, Effect sheenShader, bool glowMask = false)
	{
		Vector2 origin = (effects == SpriteEffects.FlipHorizontally) ? new(NPC.frame.Width - Components[layer].Origin.X, Components[layer].Origin.Y) : Components[layer].Origin;
		Vector2 position = basePosition + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation);
		float rotation = NPC.rotation;
		rotation += Math.Clamp(NPC.velocity.X * Components[layer].PhysicsStrength, -3, 3);
		Rectangle frame = texture.Frame(Profile.Columns, Profile.Rows, layer, currentFrame.Y);

		sheenShader.Parameters["sourceRect"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));

		//Wing special drawing
		if (layer is 1 or 10)
		{
			int WingFrame(int offset = 0) => (int)(wingFrameCounter + offset) % Profile.Rows;

			//draw a subtle afterimage of the previous frame of the wing anim- unsure of this but i thiiink it looks better for selling the super fast wing motion?
			frame = texture.Frame(Profile.Columns, Profile.Rows, layer, WingFrame(3));
			Main.EntitySpriteDraw(texture, position, frame, drawColor * WING_OPACITY * 0.16f, rotation, origin, scale, effects);

			frame = texture.Frame(Profile.Columns, Profile.Rows, layer, WingFrame(0));
			Main.EntitySpriteDraw(texture, position, frame, drawColor * WING_OPACITY, rotation, origin, scale, effects);
			return;
		}

		if (!glowMask)
			Main.EntitySpriteDraw(texture, position, frame, drawColor, rotation, origin, scale, effects);
		//Glowmask
		else
		{
			float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
			Main.EntitySpriteDraw(texture, basePosition + (origin - NPC.frame.Size() / 2).RotatedBy(NPC.rotation), frame, NPC.DrawColor(Color.White), rotation, origin, scale, effects);

			DrawHelpers.DrawOutline(Main.spriteBatch, texture, NPC.Center - Main.screenPosition, default, (offset) =>
				Main.EntitySpriteDraw(texture, position, frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, rotation, origin, scale, effects));
		}
	}
}