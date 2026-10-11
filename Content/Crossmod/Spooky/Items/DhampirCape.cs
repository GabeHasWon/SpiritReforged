using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Crossmod.Spooky.Items;

internal class DhampirCape : ModItem, IFlagged
{
	internal class DhampirCapePlayer : ModPlayer
	{
		internal bool reflectionOnly = false;

		public override void Load() => On_LegacyPlayerRenderer.DrawPlayerFull += HidePlayers;

		private void HidePlayers(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer)
		{
			bool hasCape = drawPlayer.CheckFlag("DhampirCape") == true && drawPlayer.GetModPlayer<DhampirCapePlayer>().reflectionOnly;

			if (!hasCape || Reflections.DrawingReflection || (drawPlayer.immuneTime > 0 && !drawPlayer.immuneNoBlink))
				orig(self, camera, drawPlayer);
		}
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Aglet);
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;
		Item.value = Item.buyPrice(0, 1, 0, 0);
	}

	public override void UpdateAccessory(Player player, bool hideVisual) 
	{
		player.aggro -= 400;
		player.noFallDmg = true;
		player.gravity *= 0.65f;
		player.GetModPlayer<DhampirCapePlayer>().reflectionOnly = hideVisual;
	}
}
