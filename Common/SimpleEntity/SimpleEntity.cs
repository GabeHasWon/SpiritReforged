using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.SimpleEntity;

public abstract class SimpleEntity : Entity
{
	/// <summary> Whether this entity should be saved with the current world data. </summary>
	public bool saveMe;

	public Asset<Texture2D> Texture => SimpleEntitySystem.Textures[SimpleEntitySystem.Types[GetType()]];

	public virtual string TexturePath => DrawHelpers.RequestLocal(GetType(), GetType().Name); //GetType().Namespace.Replace('.', '/') + "/" + GetType().Name;

	/// <summary> Can be used to set defaults. </summary>
	public virtual void Load() { }

	public virtual void Update() { }

	public virtual void Draw(SpriteBatch spriteBatch) { }

	public void Kill()
	{
		SimpleEntitySystem.RemoveEntity(whoAmI);
		OnKill();
	}

	public virtual void OnKill() { }

	public virtual SimpleEntity Clone() => MemberwiseClone() as SimpleEntity;
}
