using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Aether.Items;

public class GlitterPurse : ModItem
{
	public sealed class GlitterPursePlayer : ModPlayer
	{
		public bool usedGlitterPurse;

		public override void SaveData(TagCompound tag) => tag[nameof(usedGlitterPurse)] = usedGlitterPurse;
		public override void LoadData(TagCompound tag) => usedGlitterPurse = tag.GetBool(nameof(usedGlitterPurse));
	}

	public const int SlotIncrease = 2;

	public override void SetDefaults() => Item.CloneDefaults(ItemID.PeddlersSatchel);

	public override bool? UseItem(Player player)
	{
		if (player.ItemAnimationJustStarted)
		{
			GlitterPursePlayer pursePlayer = player.GetModPlayer<GlitterPursePlayer>();
			bool didUsePurse = pursePlayer.usedGlitterPurse;
			pursePlayer.usedGlitterPurse = true;

			if (!didUsePurse)
			{
				if (player.whoAmI == Main.myPlayer)
					Main.NewText(this.GetLocalizedValue("StatusText"), 50, byte.MaxValue, 130);

				return true;
			}
		}

		return false;
	}
}