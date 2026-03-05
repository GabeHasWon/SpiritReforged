using Terraria.Graphics.Effects;
using static SpiritReforged.Common.Visuals.Skies.AutoloadSkyDict;

namespace SpiritReforged.Common.Visuals.Skies;

public abstract class AutoloadedSky : CustomSky, ILoadable
{
	internal bool _isActive;

	/// <summary>
	/// The amount that the opacity of the sky increases or decreases with each tick. Defaults to 0.01f
	/// </summary>
	internal virtual float FadeSpeed { get; set; } = 0.01f;

	/// <summary>
	/// If set to true, disables vanilla sun/moon drawing. Use in combination with <see cref="DrawBelowSunMoon(SpriteBatch)"/> to create a custom sun/moon, if desired.
	/// </summary>
	internal virtual bool DisablesSunAndMoon { get; set; } = false;

	public float FadeOpacity { get; set; }

	public void Load(Mod mod)
	{
		string key = mod.Name + ":" + GetType().Name;
		SkyManager.Instance[mod.Name + ":" + GetType().Name] = (CustomSky)Activator.CreateInstance(GetType());
		LoadedSkies.Add(key, new Func<Player, bool>(ActivationCondition));
	}

	public void Unload() { }

	public override void Update(GameTime gameTime)
	{
		if (_isActive)
			FadeOpacity = Math.Min(1f, FadeSpeed + FadeOpacity);
		else
			FadeOpacity = Math.Max(0f, FadeOpacity - FadeSpeed);

		OnUpdate(gameTime);
	}

	public override void Activate(Vector2 position, params object[] args)
	{
		_isActive = true;
		OnActivate(args);
	}

	public override void Deactivate(params object[] args)
	{
		_isActive = false;
		OnDeactivate(args);
	}

	public override void Reset()
	{
		_isActive = false;
		OnReset();
	}

	public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
	{
		if (maxDepth < float.MaxValue)
			return;

		DoDraw(spriteBatch);
	}

	public override bool IsActive() => _isActive || FadeOpacity > 0;

	/// <summary>
	/// Optional hooks to run when the sky deactivates or resets.
	/// </summary>

	internal virtual void OnActivate(params object[] args) { }
	internal virtual void OnDeactivate(params object[] args) { }
	internal virtual void OnReset() { }

	/// <summary>
	/// Draws the sky, with a maxDepth check automatically applied to prevent multiple cases of it drawing.
	/// </summary>
	internal virtual void DoDraw(SpriteBatch spriteBatch) { }

	/// <summary>
	/// Layer of the sky drawn directly underneath the sun and moon. 
	/// Do note that spritebatch params will be different than from the normal Draw hook
	/// </summary>
	/// <param name="spriteBatch"></param>
	public virtual void DrawBelowSunMoon(SpriteBatch spriteBatch) { }

	/// <summary>
	/// Runs after the default update hook for skies, which now contains logic for fading in/out.
	/// </summary>
	/// <param name="gameTime"></param>
	internal virtual void OnUpdate(GameTime gameTime) { }

	/// <summary>
	/// Determines when the sky should be activated. If this returns true, then the sky will automatically fade in, and it will automatically fade out if it returns false.
	/// </summary>
	/// <param name="p"></param>
	/// <returns></returns>
	internal abstract bool ActivationCondition(Player p);
}