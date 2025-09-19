using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;

namespace SpiritReforged.Common.PrimitiveRendering;

public class TrailRenderer<T> where T : BaseTrail
{
	protected readonly List<T> Trails = [];

	public virtual void Update()
	{
		for (int i = 0; i < Trails.Count; i++)
		{
			BaseTrail trail = Trails[i];

			trail.Update();

			if (trail.CanBeDisposed)
				Trails.RemoveAt(i--);
		}
	}

	public virtual void Draw(SpriteBatch spriteBatch)
	{
		foreach (BaseTrail trail in Trails)
			trail.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
	}
}

public sealed class ProjectileTrailRenderer : TrailRenderer<BaseTrail>
{
	public readonly record struct Settings(Projectile Parent);
	private readonly List<Settings> _trailSettings = [];

	public void CreateTrail(Projectile projectile, BaseTrail trail)
	{
		Trails.Add(trail);
		_trailSettings.Add(new(projectile));
	}

	/// <summary> Dissolves any trails attached to <paramref name="projectile"/>. </summary>
	public void DissolveTrail(Projectile projectile, float dissolveSpeed = -1)
	{
		for (int i = 0; i < Trails.Count; i++)
		{
			if (projectile == _trailSettings[i].Parent)
			{
				var trail = Trails[i];

				if (dissolveSpeed > 0 && trail is VertexTrail t)
					t.DissolveSpeed = dissolveSpeed;

				trail.Dissolve();
			}
		}
	}

	public override void Update()
	{
		for (int i = 0; i < Trails.Count; i++)
		{
			BaseTrail trail = Trails[i];
			trail.Update();

			if (!_trailSettings[i].Parent.active)
				trail.Dissolve();

			if (trail.CanBeDisposed)
			{
				Trails.RemoveAt(i);
				_trailSettings.RemoveAt(i--);
			}
		}
	}
}

/// <summary> Handles Trail Renderers and related detours. Does not exist on the server. </summary>
[Autoload(Side = ModSide.Client)]
public class TrailSystem : ModSystem
{
	public static readonly Effect TrailShaders = AssetLoader.LoadedShaders["trailShaders"].Value;
	public static readonly ProjectileTrailRenderer ProjectileRenderer = new();

	public override void Load() => On_Main.DrawProjectiles += DrawTrails;
	private static void DrawTrails(On_Main.orig_DrawProjectiles orig, Main self)
	{
		ProjectileRenderer.Draw(Main.spriteBatch);
		orig(self);
	}

	public override void PreUpdateItems() => ProjectileRenderer.Update();
}