using SpiritReforged.Common.Particle;

namespace SpiritReforged;

public class SpiritReforgedSystem : ModSystem
{
	/// <summary> Called after all other systems are loaded. </summary>
	public static event Action OnLoad;
	/// <summary> Called after all other systems are unloaded. </summary>
	public static event Action OnUnload;
	/// <summary> Called after all other system content has been set up. </summary>
	public static event Action OnSetupContent;

	public override void PreUpdateItems()
	{
		if (Main.netMode != NetmodeID.Server)
		{
			AssetLoader.VertexTrailManager.UpdateTrails();
			ParticleHandler.UpdateAllParticles();
		}
	}

	public override void OnModLoad()
	{
		OnLoad?.Invoke();
		OnLoad = null;
	}

	public override void OnModUnload()
	{
		OnUnload?.Invoke();
		OnUnload = null;
	}

	public override void PostSetupContent()
	{
		OnSetupContent?.Invoke();
		OnSetupContent = null;
	}
}