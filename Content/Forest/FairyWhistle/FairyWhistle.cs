using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.Classic;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.FairyWhistle;

[FromClassic("FairyWhistleItem")]
public class FairyWhistle : ModItem
{
	public static readonly SoundStyle Whistle = new("SpiritReforged/Assets/SFX/Item/Whistle")
	{
		PitchVariance = .3f,
		Volume = 1.2f
	};

	public override void SetStaticDefaults()
	{
		MoRHelper.AddElement(Item, MoRHelper.Arcane);
		MoRHelper.AddElement(Item, MoRHelper.Nature, true);
	}

	public override void SetDefaults()
	{
		Item.damage = 4;
		Item.width = 22;
		Item.height = 18;
		Item.value = Item.sellPrice(0, 0, 0, 10);
		Item.rare = ItemRarityID.White;
		Item.mana = 12;
		Item.knockBack = 2f;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.DamageType = DamageClass.Summon;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.shoot = ModContent.ProjectileType<FairyMinion>();
		Item.UseSound = Whistle;
		Item.autoReuse = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SoundEngine.PlaySound(SoundID.Item44, player.Center);

		Projectile.NewProjectile(source, position, -Vector2.UnitY, type, damage, knockback, player.whoAmI, ai1: Main.rand.Next(3));
		Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<FairyWhistleHeld>(), 0, 0, player.whoAmI);
		return false;
	}

	public override Vector2? HoldoutOffset() => new Vector2(5, -2);
	public override void AddRecipes() => CreateRecipe().AddRecipeGroup(RecipeGroupID.Wood, 20).AddIngredient(ItemID.Acorn, 1).AddTile(TileID.WorkBenches).Register();
}

/// <summary> Used for controlling <see cref="FairyWhistle"/> item use visuals. </summary>
internal class FairyWhistleHeld : ModProjectile
{
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.FairyWhistle.DisplayName");

	public override void SetDefaults()
	{
		Projectile.timeLeft = 30;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];
		var position = owner.MountedCenter + new Vector2(12 * owner.direction, -3);

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		Projectile.Center = owner.RotatedRelativePoint(position);
		owner.heldProj = Projectile.whoAmI;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		return false;
	}
}