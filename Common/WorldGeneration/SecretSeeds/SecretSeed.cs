using SpiritReforged.Common.Visuals;
using Terraria.IO;

namespace SpiritReforged.Common.WorldGeneration.SecretSeeds;

public abstract class SecretSeed : ModType
{
	/// <summary> The names/keys of this custom seed for input, <b>Not</b> for saving. </summary>
	public abstract string[] Keys { get; }
	/// <summary> The path of the icon to display on worlds generated with this seed. </summary>
	public virtual string Icon => DrawHelpers.RequestLocal(GetType(), Name + "_Icon");

	public virtual Asset<Texture2D> GetIcon(WorldFileData data) => ModContent.Request<Texture2D>(Icon + (data.IsHardMode ? "Hallow" : string.Empty) + (data.HasCorruption ? "Corruption" : "Crimson"), AssetRequestMode.ImmediateLoad);

	protected sealed override void Register() => SecretSeedSystem.RegisterSeed(this);
	public sealed override void SetupContent() => SetStaticDefaults();
}