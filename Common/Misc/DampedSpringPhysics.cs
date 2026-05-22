using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;

namespace SpiritReforged.Common.Misc;

public class DampedSpringPhysics
{
	public Vector2 value;
	public Vector2 velocity;
	public Vector2? lastTarget = null;

	public float damping;
	public float frequency;
	public float reaction;

	public float k1;
	public float k2;
	public float k3;
	public float maxValues;

	/// <summary>
	/// An utility class that calculates a value accelerating towards a goal in a damped / springlike fashion
	/// </summary>
	/// <param name="damping">How fast the spring comes to settle down<br/>
	/// If the value is zero, the system will never stop oscillating and the value is undamped<br/>
	/// If the value is between zero and one, the system will end up settling down<br/>
	/// If the value is higher than one, the system will not oscillate and immediately settle to the target value</param>
	/// <param name="frequency">The frequency at which the oscillation happens, and the speed at which the curve responds.
	/// Basically, scales the curve horizontally, without changing its vertical shape</param>
	/// <param name="reaction">Controls the initial response of the system.<br/>When = 0, the system takes a while to adapt.<br/> When positive, the response will be more immediate, and if superior to 1, it will overshoot the target.<br/> If inferior to 0, the system will start with an initial windup</param>
	/// <param name="maxValues">The maximum value of the tracker</param>
	public DampedSpringPhysics(float damping, float frequency, float reaction, float maxValues = 80)
	{
		this.damping = damping;
		this.frequency = frequency;
		this.reaction = reaction;
		this.maxValues = maxValues;

		RecalculateFactors();
	}

	//Separated for debug purposes
	public void RecalculateFactors()
	{
		k1 = damping / (MathHelper.Pi * frequency);
		k2 = 1 / MathF.Pow(MathHelper.TwoPi * frequency, 2f);
		k3 = (reaction * damping) / (MathHelper.TwoPi * frequency);
	}

	/// <summary>
	/// Updates the simulation
	/// </summary>
	/// <param name="target">This is the target value our spring wants to reach</param>
	/// <param name="targetAcceleration">This is the difference between the last position of our target and the current one<br/>
	/// Leave empty if you want to use the last recorded difference in targets </param>
	public void Update(Vector2 target, Vector2? targetAcceleration = null)
	{
		if (value.HasNaNs() || value.HasInfinities())
			value = target;
		if (velocity.HasNaNs() || value.HasInfinities())
			velocity = Vector2.Zero;

		if (!targetAcceleration.HasValue)
		{
			if (lastTarget == null)
				targetAcceleration = Vector2.Zero;
			else
				targetAcceleration = target - lastTarget.Value;
		}

		value += velocity;
		velocity += (target + k3 * targetAcceleration.Value - value - k1 * velocity) / k2;

		value.X = Math.Clamp(value.X, -maxValues, maxValues);
		value.Y = Math.Clamp(value.Y, -maxValues, maxValues);
		velocity.X = Math.Clamp(velocity.X, -maxValues, maxValues);
		velocity.Y = Math.Clamp(velocity.Y, -maxValues, maxValues);

		lastTarget = target;
	}

	/// <summary>
	/// Updates the simulation accounting for deltatime
	/// </summary>
	/// <param name="target">This is the target value our spring wants to reach</param>
	/// <param name="targetAcceleration">This is the difference between the last position of our target and the current one<br/>
	/// Leave empty if you want to use the last recorded difference in targets </param>
	public void Update(float deltatime, Vector2 target, Vector2? targetAcceleration = null)
	{
		if (value.HasNaNs() || value.HasInfinities())
			value = target;
		if (velocity.HasNaNs() || value.HasInfinities())
			velocity = Vector2.Zero;

		if (!targetAcceleration.HasValue)
		{
			if (lastTarget == null)
				targetAcceleration = Vector2.Zero;
			else
				targetAcceleration = target - lastTarget.Value;
		}

		deltatime *= 10f;
		float k2Stable = Math.Max(k2, 1.1f * (deltatime * deltatime / 4f + deltatime * k1 / 2f)); //Clamp k2 to guarantee stability)

		value += velocity * deltatime;
		velocity += (target + k3 * targetAcceleration.Value - value - k1 * velocity) * deltatime / k2Stable;

		value.X = Math.Clamp(value.X, -maxValues, maxValues);
		value.Y = Math.Clamp(value.Y, -maxValues, maxValues);
		velocity.X = Math.Clamp(velocity.X, -maxValues, maxValues);
		velocity.Y = Math.Clamp(velocity.Y, -maxValues, maxValues);

		lastTarget = target;
	}
}