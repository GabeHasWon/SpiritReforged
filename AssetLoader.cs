using SpiritReforged.Common.PrimitiveRendering;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Core;
using SpiritReforged;

internal static class AssetLoader
{
	public static TrailManager VertexTrailManager;
	public static BlendState NonPremultipliedAlphaFix;

	public static BasicEffect BasicShaderEffect;
	public static IDictionary<string, Asset<Texture2D>> LoadedTextures = new Dictionary<string, Asset<Texture2D>>();
	public static IDictionary<string, Asset<Effect>> LoadedShaders = new Dictionary<string, Asset<Effect>>();

	public static string EmptyTexture => "Terraria/Images/NPC_0";

	public static void Load(Mod mod)
	{
		if (Main.dedServ) //dont do this on the server because it will DIE
			return;

		ShaderHelpers.GetWorldViewProjection(out Matrix view, out Matrix projection);
		Main.QueueMainThreadAction(() => BasicShaderEffect = new BasicEffect(Main.graphics.GraphicsDevice)
		{
			VertexColorEnabled = true,
			View = view,
			Projection = projection
		});

		NonPremultipliedAlphaFix = new BlendState
		{
			ColorSourceBlend = Blend.SourceAlpha,
			AlphaSourceBlend = Blend.SourceAlpha,
			ColorDestinationBlend = Blend.One,
			AlphaDestinationBlend = Blend.InverseSourceAlpha,
		};

		var tmodfile = (TmodFile)typeof(SpiritReforgedMod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SpiritReforgedMod.Instance);
		var files = (IDictionary<string, TmodFile.FileEntry>)typeof(TmodFile).GetField("files", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tmodfile);
		string assetsDirectory = "Assets/";
		foreach (KeyValuePair<string, TmodFile.FileEntry> kvp in files.Where(x => x.Key.Contains(assetsDirectory)))
		{
			//Loading textures
			string textureDirectory = assetsDirectory + "Textures/";
			if (kvp.Key.Contains(textureDirectory) && kvp.Key.Contains(".rawimg"))
			{
				string texturePath = RemoveExtension(kvp.Key, ".rawimg");
				string textureKey = RemoveDirectory(texturePath, textureDirectory);

				LoadedTextures.Add(textureKey, mod.Assets.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad));
			}

			//Loading shaders
			string shaderDirectory = assetsDirectory + "Shaders/";
			if (kvp.Key.Contains(shaderDirectory) && kvp.Key.Contains(".xnb"))
			{
				string shaderPath = RemoveExtension(kvp.Key, ".xnb");
				string shaderKey = RemoveDirectory(shaderPath, shaderDirectory);

				LoadedShaders.Add(shaderKey, mod.Assets.Request<Effect>(shaderPath, AssetRequestMode.ImmediateLoad));
			}
		}

		VertexTrailManager = new TrailManager();

		//Register some vanilla textures under our own system for convenience
		LoadedTextures.Add("FlameTrail", TextureAssets.Extra[189]);
		LoadedTextures.Add("SwirlNoise", TextureAssets.Extra[193]);
		LoadedTextures.Add("EnergyTrail", TextureAssets.Extra[194]);
		LoadedTextures.Add("GlowTrail_2", TextureAssets.Extra[197]);
	}

	/// <summary>
	/// Removes the extension of the file- turns "Assets/Textures/Bloom.png" to "Assets/Textures/Bloom", for example
	/// </summary>
	/// <param name="input"></param>
	/// <param name="extensionType"></param>
	/// <returns></returns>
	private static string RemoveExtension(string input, string extensionType) => input[..^extensionType.Length];

	/// <summary>
	/// Removes the directories from the string used for the key- turning "Assets/Textures/Bloom" to "Bloom"
	/// </summary>
	/// <param name="input"></param>
	/// <param name="directory"></param>
	/// <returns></returns>
	private static string RemoveDirectory(string input, string directory) => input.Remove(0, directory.Length);

	public static void Unload()
	{
		if (Main.dedServ)
			return;

		VertexTrailManager = null;
		BasicShaderEffect = null;
		LoadedTextures = new Dictionary<string, Asset<Texture2D>>();
		LoadedShaders = new Dictionary<string, Asset<Effect>>();
	}
}