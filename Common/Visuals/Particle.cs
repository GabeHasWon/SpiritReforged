using SpiritReforged.Common.Misc;
using System.Reflection;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Common.Visuals;

/// <summary> Provides additional utilities over <see cref="ABasicParticle"/>.<para/>
/// See <see cref="ParticleRenderers"/> for more info. </summary>
public abstract class Particle : IPooledParticle, IParticle
{
	private static readonly Dictionary<string, Asset<Texture2D>> _textureByName;

	public virtual string TexturePath => DrawHelpers.RequestLocal(GetType(), GetType().Name);

	public Texture2D Texture
	{
		get
		{
			string name = GetType().Name;
			if (_textureByName.TryGetValue(name, out Asset<Texture2D> textureAsset))
			{
				return textureAsset.Value;
			}
			else
			{
				Asset<Texture2D> newTextureAsset = ModContent.Request<Texture2D>(TexturePath);
				_textureByName.Add(name, newTextureAsset);

				return newTextureAsset.Value;
			}
		}
	}

	public float Progress => (float)TimeActive / TimeMax;

	public bool IsRestingInPool { get; protected set; }

	public bool ShouldBeRemovedFromRenderer { get; protected set; }

	public int TimeMax;
	public int TimeActive;
	public float Rotation;
	public Vector2 LocalPosition;
	public Vector2 Velocity;
	public Vector2 Scale;

	public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch) { }

	public virtual void Update(ref ParticleRendererSettings settings)
	{
		if (TimeMax > 0 && ++TimeActive >= TimeMax)
			ShouldBeRemovedFromRenderer = true;
	}

	public void RestInPool() => IsRestingInPool = true;
	public void FetchFromPool() => IsRestingInPool = false; //Reset
}

public sealed class ParticleRenderers : ModSystem
{
	public static ParticleRenderer[] Renderers { get; private set; }

	public static readonly ParticleRenderer OverInventory = new();
	public static readonly ParticleRenderer OverHealthBars = new();
	public static readonly ParticleRenderer UnderProjectiles = new();
	public static readonly ParticleRenderer UnderNPCs = new(), OverNPCs = new();
	public static readonly ParticleRenderer OverPlayers = new();
	public static readonly ParticleRenderer OverItems = new();
	public static readonly ParticleRenderer UnderSolids = new(), OverSolids = new();
	public static readonly ParticleRenderer UnderWalls = new();

	public override void Load()
	{
		On_Main.UpdateParticleSystems += UpdateParticles; //Update hooks

		On_Main.DoDraw_WallsAndBlacks += PreDrawWalls;  //Drawing hooks
		On_Main.DoDraw_Tiles_NonSolid += PreDrawSolid;
		On_Main.DoDraw_Tiles_Solid += PostDrawSolid;
		On_Main.DrawNPCs += AroundNPC;
		On_Main.DrawItems += PostDrawItems;
		On_Main.DrawProjectiles += PreDrawProjectiles;
		On_Main.DrawInfernoRings += PostDrawPlayers;
		On_Main.DrawInventory += PostDrawInventory;
		On_Main.DrawInterface_14_EntityHealthBars += PostDrawHealthBars;

		List<ParticleRenderer> renderers = []; //Populate Renderers array
		foreach (FieldInfo field in GetType().GetFields())
		{
			if (field.IsStatic && field.FieldType == typeof(ParticleRenderer))
				renderers.Add((ParticleRenderer)field.GetValue(null));
		}

		Renderers = renderers.ToArray();
	}

	private static void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		OverInventory.Update();
		OverHealthBars.Update();

		orig(self);
	}

	private static void PreDrawWalls(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
	{
		if (UnderWalls.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.BackgroundViewMatrix.TransformationMatrix);

			UnderWalls.Draw(spriteBatch);
			spriteBatch.RestartToDefault();
		}

		orig(self);
	}

	private static void PreDrawSolid(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);
		UnderSolids.Draw(Main.spriteBatch);
	}

	private static void PostDrawSolid(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
	{
		orig(self);

		if (OverSolids.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.GameViewMatrix.TransformationMatrix);

			OverSolids.Draw(spriteBatch);
			spriteBatch.End();
		}
	}

	private static void AroundNPC(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		UnderNPCs.Draw(Main.spriteBatch);
		orig(self, behindTiles);
		OverNPCs.Draw(Main.spriteBatch);
	}

	private static void PostDrawItems(On_Main.orig_DrawItems orig, Main self)
	{
		orig(self);
		OverItems.Draw(Main.spriteBatch);
	}

	private static void PreDrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
	{
		if (UnderProjectiles.Particles.Count > 0)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
			UnderProjectiles.Draw(spriteBatch);
			spriteBatch.End();

			orig(self);
		}
		else
		{
			orig(self);
		}
	}

	private static void PostDrawPlayers(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		orig(self);

		if (OverPlayers.Particles.Count > 0) //Avoid restarting the SpriteBatch if there's nothing to draw
		{
			SpriteBatch spriteBatch = Main.spriteBatch;

			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, default, default, RasterizerState.CullCounterClockwise, default, Main.GameViewMatrix.TransformationMatrix);

			OverPlayers.Draw(spriteBatch);
			spriteBatch.RestartToDefault();
		}
	}

	private static void PostDrawInventory(On_Main.orig_DrawInventory orig, Main self)
	{
		orig(self);
		OverInventory.Draw(Main.spriteBatch);
	}

	private static void PostDrawHealthBars(On_Main.orig_DrawInterface_14_EntityHealthBars orig, Main self)
	{
		orig(self);
		OverHealthBars.Draw(Main.spriteBatch);
	}
}