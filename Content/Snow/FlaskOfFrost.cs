using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Content.Snow.Frostbite;

namespace SpiritReforged.Content.Snow;

[AutoloadBuff]
public class FlaskOfFrost : ModItem
{
	internal class FrostImbuePlayer : ModPlayer
	{
		public bool HasBuff => Player.HasBuff(BuffType);

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (HasBuff && item.DamageType.CountsAsClass<MeleeDamageClass>())
				target.AddBuff(ModContent.BuffType<Frozen>(), 60 * Main.rand.Next(3, 7));
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (HasBuff && (proj.DamageType.CountsAsClass<MeleeDamageClass>() || ProjectileID.Sets.IsAWhip[proj.type]) && !proj.noEnchantments)
				target.AddBuff(ModContent.BuffType<Frozen>(), 60 * Main.rand.Next(3, 7));
		}

		public override void MeleeEffects(Item item, Rectangle hitbox)
		{
			if (HasBuff && item.DamageType.CountsAsClass<MeleeDamageClass>() && !item.noMelee && !item.noUseGraphic && Main.rand.NextBool(5))
				Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.GemSapphire).noGravity = true;
		}

		public override void EmitEnchantmentVisualsAt(Projectile projectile, Vector2 boxPosition, int boxWidth, int boxHeight)
		{
			if (HasBuff && (projectile.DamageType.CountsAsClass<MeleeDamageClass>() || ProjectileID.Sets.IsAWhip[projectile.type]) && !projectile.noEnchantments && Main.rand.NextBool(5))
				Dust.NewDustDirect(boxPosition, boxWidth, boxHeight, DustID.GemSapphire).noGravity = true;
		}
	}

	public static int BuffType { get; private set; }

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 20;

		BuffType = BuffAutoloader.SourceToType[GetType()];
		Main.persistentBuff[BuffType] = true;
		Main.meleeBuff[BuffType] = true;
	}

	public override void SetDefaults()
	{
		Item.DefaultToFood(26, 32, BuffType, Item.flaskTime, true);
		Item.rare = ItemRarityID.LightRed;
		Item.value = Item.sellPrice(silver: 2);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BottledWater).AddIngredient(ItemID.Shiverthorn, 3).AddTile(TileID.ImbuingStation).Register();
}