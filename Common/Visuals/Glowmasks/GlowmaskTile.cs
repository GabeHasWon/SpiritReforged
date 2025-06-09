using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SpiritReforged.Common.TileCommon;
using System.Linq;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.Visuals.Glowmasks;

internal class GlowmaskTile : GlobalTile
{
	public static Dictionary<int, GlowmaskInfo> TileIdToGlowmask = [];

	public override void Load() => IL_TileDrawing.GetTileDrawData += InjectGlowmaskData;

	private void InjectGlowmaskData(ILContext il)
	{
		ILCursor c = new(il);

		c.Index = c.Instrs.Count - 1; //Move to the end

		var p_typeCache = c.Method.Parameters.Where(x => x.Name == "typeCache").FirstOrDefault();
		var p_glowTexture = c.Method.Parameters.Where(x => x.Name == "glowTexture").FirstOrDefault();
		var p_glowColor = c.Method.Parameters.Where(x => x.Name == "glowColor").FirstOrDefault();
		var p_glowSourceRect = c.Method.Parameters.Where(x => x.Name == "glowSourceRect").FirstOrDefault();

		if (p_typeCache == default)
		{
			SpiritReforgedMod.Instance.LogIL("Inject Glowmask Data", "Parameter 'typeCache' not found.");
			return;
		}

		if (p_glowTexture == default)
		{
			SpiritReforgedMod.Instance.LogIL("Inject Glowmask Data", "Parameter 'glowTexture' not found.");
			return;
		}

		if (p_glowColor == default)
		{
			SpiritReforgedMod.Instance.LogIL("Inject Glowmask Data", "Parameter 'glowColor' not found.");
			return;
		}

		if (p_glowSourceRect == default)
		{
			SpiritReforgedMod.Instance.LogIL("Inject Glowmask Data", "Parameter 'glowSourceRect' not found.");
			return;
		}

		c.Emit(OpCodes.Ldarg_S, p_typeCache); //The tile type
		c.EmitLdarg1(); //i
		c.EmitLdarg2(); //j

		c.Emit(OpCodes.Ldarg_S, p_glowTexture); //Glowmask
		c.Emit(OpCodes.Ldarg_S, p_glowColor); //Glow color
		c.Emit(OpCodes.Ldarg_S, p_glowSourceRect); //Glow frame

		c.EmitDelegate(ModifyData);
	}

	//Modify GetTileDrawData to include our own data, which is more versatile than DrawEffects. Useful for when things like wind-affected tiles and vines are drawn using vanilla methods
	private void ModifyData(int typeCache, int i, int j, ref Texture2D glowTexture, ref Color glowColor, ref Rectangle glowSourceRect)
	{
		if (TileIdToGlowmask.TryGetValue(typeCache, out GlowmaskInfo value) && value.DrawAutomatically)
		{
			var tile = Main.tile[i, j];

			if (tile.Slope == SlopeType.Solid && !tile.IsHalfBlock) //This method can't draw slopes
			{
				glowTexture = TileIdToGlowmask[typeCache].Glowmask.Value;
				glowColor = TileIdToGlowmask[typeCache].GetDrawColor(new Point(i, j));

				int addFrameX = 0;
				int addFrameY = 0;

				TileLoader.SetAnimationFrame(typeCache, i, j, ref addFrameX, ref addFrameY);
				var source = new Rectangle(tile.TileFrameX, tile.TileFrameY + addFrameY, 16, 16);

				glowSourceRect = source;
			}
		}
	}

	public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (TileIdToGlowmask.TryGetValue(type, out var glow) && glow.DrawAutomatically)
		{
			var tile = Main.tile[i, j];

			if (tile.Slope != SlopeType.Solid || tile.IsHalfBlock) //This method can draw slopes
			{
				var pos = TileExtensions.DrawPosition(i, j);
				TileExtensions.DrawSloped(i, j, glow.Glowmask.Value, glow.GetDrawColor(new Point(i, j)), Vector2.Zero);
			}
		}
	}
}
