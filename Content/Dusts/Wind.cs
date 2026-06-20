using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Dusts;

public class Wind : ModDust
{
	public class WindAnchor
	{
		public float turnRate;
		public float offset;
		public Vector2 offsetDir;
		public Vector2 anchor;

		public WindAnchor(Vector2 origin, Vector2 velocity, Vector2 position)
		{
			float length = velocity.Length();

			velocity *= 1f / length;
			if (velocity.HasNaNs())
				velocity = new Vector2(0, -1);

			turnRate = 0.06f + Main.rand.NextFloat(0.04f);
			turnRate *= length > 4 ? length : 4;

			if ((position - origin).X < velocity.X)
			{
				turnRate = -turnRate;
				offsetDir = -velocity.TurnLeft();
			}
			else
			{
				offsetDir = -velocity.TurnRight();
			}

			offset = 2 + Main.rand.NextFloat(2);
			anchor = offsetDir * offset;
			anchor += position;
		}

		public WindAnchor(Vector2 origin, Vector2 position)
		{
			turnRate = 0.06f + Main.rand.NextFloat(0.04f);
			turnRate *= 6;

			bool left = position.X - origin.X < 0;
			if (left)
			{
				turnRate = -turnRate;
				offsetDir = new Vector2(1, 0);
			}
			else
				offsetDir = new Vector2(-1, 0);

			offset = 2 + Main.rand.NextFloat(2);
			anchor = offsetDir * offset;
			anchor += position;
		}
	}

	public override void OnSpawn(Dust dust)
	{
		dust.noGravity = true;
		dust.velocity = Vector2.Zero;
	}

	public override bool Update(Dust dust)
	{
		if (dust.customData is null || dust.alpha >= 255)
		{
			dust.active = false;
			return false;
		}

		if (dust.customData is WindAnchor data)
		{
			data.anchor += dust.velocity;
			dust.position = data.anchor + (data.offsetDir * data.offset).RotatedBy(dust.rotation);
			dust.rotation += data.turnRate;

			dust.alpha += 5;
			dust.scale *= 0.98f;
			dust.velocity *= 0.95f;
			data.offset += 0.4f;
			data.turnRate *= 0.92f;

			return false;
		}

		return true;
	}
}