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