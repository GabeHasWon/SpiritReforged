using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon;

public interface IDrawHeld
{
	public sealed class DrawHeldLoader : ILoadable
	{
		public void Load(Mod mod) => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;

		private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
		{
			if (drawinfo.drawPlayer.HeldItem.ModItem is IDrawHeld iHeld)
			{
				iHeld.DrawHeld(ref drawinfo);
				return; //Skips orig
			}

			orig(ref drawinfo);
		}

		public void Unload() { }
	}

	public void DrawHeld(ref PlayerDrawSet drawinfo);
}