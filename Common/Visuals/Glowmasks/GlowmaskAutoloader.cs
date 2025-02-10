﻿using System.Linq;

namespace SpiritReforged.Common.Visuals.Glowmasks;

internal class GlowmaskAutoloader : ModSystem
{
	public override void PostSetupContent()
	{
		var types = Mod.GetContent().Where(x => Attribute.IsDefined(x.GetType(), typeof(AutoloadGlowmaskAttribute)));

		foreach (var type in types)
		{
			Func<object, Color> color = AutoloadGlowmaskAttribute.GetAttributeInfo(Mod, type.GetType(), out bool autoDraw);

			if (type is ModNPC npc)
			{
				int id = npc.Type;
				if (TryGetGlowmask(ModContent.GetModNPC(id).Texture, out var glowMask))
					GlowmaskNPC.NpcIdToGlowmask.Add(id, new(glowMask, color, autoDraw));
			}

			else if (type is ModTile tile)
			{
				int id = tile.Type;
				if (TryGetGlowmask(ModContent.GetModTile(id).Texture, out var glowMask))
					GlowmaskTile.TileIdToGlowmask.Add(id, new(glowMask, color, autoDraw));
			}

			else if (type is ModProjectile projectile)
			{
				int id = projectile.Type;
				if (TryGetGlowmask(ModContent.GetModProjectile(id).Texture, out var glowMask))
					GlowmaskProjectile.ProjIdToGlowmask.Add(id, new(glowMask, color, autoDraw));
			}

			else if (type is ModItem item)
			{
				int id = item.Type;
				if (TryGetGlowmask(ModContent.GetModItem(id).Texture, out var glowMask))
					GlowmaskItem.ItemIdToGlowmask.Add(id, new(glowMask, color, autoDraw));
			}
		}
	}

	private static bool TryGetGlowmask(string texture, out Asset<Texture2D> asset)
	{
		if (ModContent.RequestIfExists(texture + "_Glow", out asset))
			return true;

		if (ModContent.RequestIfExists(texture + "_glow", out asset))
			return true;

		return false;
	}
}
