using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static AssGen.Assets;
using static SpiritReforged.Content.SaltFlats.Items.MahakalaMaskBlue;

namespace SpiritReforged.Content.SaltFlats.Items;
public class GnarledRod : ModItem
{

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.WoodFishingPole);
		Item.fishingPole = 20;
		Item.shootSpeed = 11f;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(0, 1, 40, 0);
		Item.shoot = ModContent.ProjectileType<GnarledRodBobber>();
	}
	public override void HoldItem(Player player)
	{
		player.accFishingLine = true;
		player.GetModPlayer<GnarledRodPlayer>().crateBoost = true;
	}

	public override void ModifyFishingLine(Projectile bobber, ref Vector2 lineOriginOffset, ref Color lineColor)
	{
		lineOriginOffset = new Vector2(43, -30);
		lineColor = new Color(223, 199, 237);
	}
}

public class GnarledRodBobber : ModProjectile
{
	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.BobberWooden);
		DrawOriginOffsetY = -8;
	}
}

public class GnarledRodPlayer : ModPlayer
{
	public bool crateBoost;

	public override void ResetEffects() => crateBoost = false;
	public override void ModifyFishingAttempt(ref FishingAttempt attempt)
	{
		float lerpValue = Utils.GetLerpValue(20, 374, Player.fishingSkill, true); //min with fishing rod, max fishing power
		int crateOdds = (int)MathHelper.Lerp(15, 4, lerpValue);

		if (crateBoost)
		{
			if (!attempt.crate && Main.rand.NextBool(crateOdds))
				attempt.crate = true;
		}
	}
}