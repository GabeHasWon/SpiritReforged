using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.GildedScarab;

public class GildedScarab : EquippableItem
{
	public override void SetDefaults()
	{
		Item.width = 34;
		Item.height = 32;
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
		Item.value = Item.sellPrice(gold: 1);
	}
}

internal class GildedScarabPlayer : ModPlayer
{
	public int ScarabDefense { get; private set; }

	public float opacity;
	public float visualCounter;

	public override void UpdateDead() => UpdateVisuals();
	public override void PostUpdateEquips() => UpdateVisuals();

	private void UpdateVisuals()
	{
		if (Player.HasBuff<GildedScarabBuff>() && !Player.dead)
		{
			opacity = MathHelper.Min(opacity + .05f, 1);
		}
		else
		{
			opacity = MathHelper.Max(opacity - .05f, 0);
			ScarabDefense = 0;
		}

		if (opacity > 0)
			visualCounter = (visualCounter + 1f / ScarabLayerBase.FrameDuration) % ScarabLayerBase.NumFramesY;
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (Player.HasEquip<GildedScarab>())
		{
			if (info.DamageSource.SourceOtherIndex is 3 or 2) //Lava or spike damage
				return;

			if (!Player.HasBuff(ModContent.BuffType<GildedScarabBuff>()))
				SoundEngine.PlaySound(SoundID.Item76);

			ScarabDefense = Math.Clamp(ScarabDefense + (int)(info.Damage / 8f), 5, 50);
			Player.AddBuff(ModContent.BuffType<GildedScarabBuff>(), 300);
		}
	}
}