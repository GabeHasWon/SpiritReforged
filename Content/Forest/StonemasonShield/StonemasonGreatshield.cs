using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.StonemasonShield;

internal class StonemasonGreatshield : GreatshieldItem
{
	internal class StonemasonBuff : ModBuff
	{
		public override void Update(Player player, ref int buffIndex)
		{
			player.buffTime[buffIndex]++;
			player.GetKnockback(DamageClass.Generic) += 1;
		}
	}

	internal class StonemasonPlayer : ModPlayer
	{
		public override void OnHitAnything(float x, float y, Entity victim) => Player.ClearBuff(ModContent.BuffType<StonemasonBuff>());
	}

	public override GreatshieldAltInfo Info => new(20, 60, 120, 40);

	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<GreatshieldClass>();
		Item.defense = 2;
		Item.damage = 4;
		Item.useTime = 35;
		Item.useAnimation = 20;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.useStyle = -1;
		Item.shoot = ModContent.ProjectileType<GreatshieldHitbox>();
		Item.knockBack = 12;
	}

	public override void ModifyLayerDrawing(ref DrawData data, bool isGuard) => data.position.Y -= 4;

	public override void OnBlock(Player player, Player.HurtInfo info) => player.AddBuff(ModContent.BuffType<StonemasonBuff>(), 2);
}
