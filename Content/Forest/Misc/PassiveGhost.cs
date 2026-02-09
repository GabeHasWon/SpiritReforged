using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

public class PassiveGhost : ModNPC
{
	public readonly record struct NPCTracker
	{
		/// <summary> Stores town NPC types and whether they are dead. </summary>
		public readonly Dictionary<int, bool> queuedByNPC = [];

		public NPCTracker(params int[] types)
		{
			foreach (int type in types)
				queuedByNPC.Add(type, false);
		}

		public readonly void FindAvailableNPCs(out List<int> queuedTypes)
		{
			queuedTypes = [];
			foreach (int type in queuedByNPC.Keys)
			{
				if (queuedByNPC[type] = Main.townNPCCanSpawn[type] && !NPC.AnyNPCs(type))
					queuedTypes.Add(type);
			}
		}
	}

	public static readonly Asset<Texture2D> ChatTexture = DrawHelpers.RequestLocal<PassiveGhost>("EmptyChat", false);
	public static NPCTracker GhostNPCTracker { get; private set; }
	private static readonly HashSet<NPC> NPCBatch = [];

	public override string Texture => AssetLoader.EmptyTexture;

	public int NPCTypeToCopy { get; private set; }
	private bool _showingDialogueBubble;

	public override void Load() => On_Main.DrawNPCs += DrawBatch;
	private static void DrawBatch(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		orig(self, behindTiles);

		if (NPCBatch.Count == 0)
			return; //Nothing to draw; don't restart the spritebatch

		SpriteBatch spriteBatch = Main.spriteBatch;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

		foreach (NPC npc in NPCBatch) //Draw all shader-affected NPCs
		{
			if (npc.ModNPC is not PassiveGhost passiveGhost)
				continue;

			Texture2D texture = TextureAssets.Npc[passiveGhost.NPCTypeToCopy].Value;
			Color alphaColor = npc.GetAlpha(Color.White);
			Rectangle source = npc.frame;
			Vector2 position = npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY - (source.Height - npc.height) / 2 + 2);
			SpriteEffects effects = (npc.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			Effect effect = AssetLoader.LoadedShaders["GhostShader"].Value;
			effect.Parameters["dimensions"].SetValue(texture.Size());
			effect.Parameters["gradientSource"].SetValue(new Vector4(source.X, source.Y, source.Width, source.Height));
			effect.Parameters["outlineColor"].SetValue(alphaColor.ToVector4());
			effect.Parameters["fadeStrength"].SetValue(1.2f);
			effect.Parameters["displacement"].SetValue(new Vector2(0.5f + (float)Math.Sin(Main.timeForVisualEffects / 120.0) * 0.5f, 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 90.0) * 0.5f));
			effect.Parameters["distortionStrength"].SetValue(new Vector2(0, 0.002f));
			effect.Parameters["distortionTexture"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
			effect.CurrentTechnique.Passes[0].Apply();

			Main.EntitySpriteDraw(texture, position, source, alphaColor, npc.rotation, source.Size() / 2, npc.scale, effects);
		}

		spriteBatch.End();
		spriteBatch.BeginDefault();

		NPCBatch.Clear();
	}

	public override void SetStaticDefaults()
	{
		List<int> npcTypes = [];

		for (int type = 0; type < NPCLoader.NPCCount; type++)
		{
			if (ContentSamples.NpcsByNetId.TryGetValue(type, out NPC npc) && npc.townNPC)
				npcTypes.Add(type);
		}

		GhostNPCTracker = new(npcTypes.ToArray());
	}

	public override void SetDefaults()
	{
		NPC.friendly = true;
		NPC.aiStyle = NPCAIStyleID.Passive;
		NPC.lifeMax = 250;
		NPC.dontTakeDamage = true;
		NPC.immortal = true;
		NPC.width = 20;
		NPC.height = 40;
		NPC.alpha = 255;
	}

	public override void OnSpawn(IEntitySource source)
	{
		GhostNPCTracker.FindAvailableNPCs(out var queuedTypes);
		if (queuedTypes.Count > 0)
		{
			NPCTypeToCopy = queuedTypes[WorldGen.genRand.Next(queuedTypes.Count)];
			NPC.netUpdate = true;
		}
		else
		{
			NPCTypeToCopy = NPCID.BestiaryGirl;
		}
	}

	public override void AI()
	{
		if (Main.dayTime)
		{
			if ((NPC.Opacity -= 1 / 120f) <= 0)
				NPC.active = false; //Fade out at dawn
		}
		else
		{
			float desiredOpacity = 1f - Math.Clamp(Main.LocalPlayer.DistanceSQ(NPC.Center) / (100f * 100f), 0, 1);
			NPC.Opacity = MathHelper.Lerp(NPC.Opacity, desiredOpacity, 0.1f);
		}

		GhostNPCTracker.queuedByNPC[NPCTypeToCopy] = false; //Prevents other ghosts of this type from spawning
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frame.Width = 40;

		if (!Main.dedServ && TextureAssets.Npc[NPCTypeToCopy].IsLoaded)
			NPC.frame.Height = TextureAssets.Npc[NPCTypeToCopy].Height() / Main.npcFrameCount[NPCTypeToCopy];

		NPC.VanillaFindFrame(NPC.frame.Height, true, NPCTypeToCopy);
	}

	public override bool PreHoverInteract(bool mouseIntersects)
	{
		Main.HoveringOverAnNPC = true;
		_showingDialogueBubble = true;

		return false;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPCBatch.Add(NPC);

		if (_showingDialogueBubble)
		{
			Texture2D texture = ChatTexture.Value;
			SpriteEffects effect = (NPC.spriteDirection == 1) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 origin = (effect == SpriteEffects.None) ? texture.Bounds.BottomLeft() : texture.Bounds.BottomRight();
			Vector2 position = ((effect == SpriteEffects.None) ? NPC.Hitbox.TopRight() - new Vector2(7, 0) : NPC.Hitbox.TopLeft() + new Vector2(9, 0)) - screenPos;

			Main.EntitySpriteDraw(texture, position, null, Color.White, 0, origin, 1, effect);

			_showingDialogueBubble = false;
		}

		return false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (!Main.dayTime && !spawnInfo.Water && spawnInfo.EventSafe() && spawnInfo.Player.ZoneGraveyard)
		{
			GhostNPCTracker.FindAvailableNPCs(out var queuedTypes);
			return (queuedTypes.Count == 0) ? 0 : 0.5f;
		}

		return 0;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(NPCTypeToCopy);
	public override void ReceiveExtraAI(BinaryReader reader) => NPCTypeToCopy = reader.ReadInt32();
}