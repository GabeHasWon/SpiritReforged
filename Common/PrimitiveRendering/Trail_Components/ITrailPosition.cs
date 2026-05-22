namespace SpiritReforged.Common.PrimitiveRendering.Trail_Components;

public interface ITrailPosition
{
	Vector2 GetNextTrailPosition();
}

public readonly struct EntityTrailPosition(Entity entity) : ITrailPosition
{
	public readonly Entity Entity = entity;
	public Vector2 GetNextTrailPosition() => Entity.Center;
}

public readonly struct ProjectileOffsetTrailPosition(Projectile entity, Vector2 offset, float rotationOffset = 0f) : ITrailPosition
{
	private readonly Vector2 offset = offset;
	public readonly Projectile Projectile = entity;

	public Vector2 GetNextTrailPosition() => Projectile.Center + offset.RotatedBy(Projectile.rotation + rotationOffset);
}