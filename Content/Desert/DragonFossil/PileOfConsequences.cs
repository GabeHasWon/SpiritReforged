using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class PileOfConsequences : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<Dragonsong>();
	public override void SetDefaults() => Item.rare = ItemRarityID.Gray;

	public override void UpdateInventory(Player player)
	{
		int type = ModContent.ProjectileType<PileOfConsequencesPet>();

		if (player.ownedProjectileCounts[type] == 0)
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, type, 0, 0, player.whoAmI);
	}
}

public class PileOfConsequencesPet : ModProjectile
{
	public override string Texture => ModContent.GetInstance<TinyDragonPet>().Texture;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 2;
		ProjectileID.Sets.TrailCacheLength[Type] = 4;
	}

	public override void AI()
	{
		Projectile.UpdateFrame(20, maxFrame: 5);

		Player owner = Main.player[Projectile.owner];
		Vector2 restingSpot = owner.Center + new Vector2(60 * -owner.direction, -60);
		float distance = Projectile.Distance(restingSpot);
		var result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(restingSpot) * Math.Clamp(distance / 16f, 0, 10), 0.1f);

		if (distance < 16)
			result *= 0.9f;

		if (!result.HasNaNs())
			Projectile.velocity = result;

		int direction = Math.Sign(Projectile.velocity.X);
		Projectile.direction = Projectile.spriteDirection = (result.Length() < 0.2f) ? owner.direction : direction;
		Projectile.rotation = Projectile.velocity.Y * 0.1f * Projectile.direction;
		bool hasItem = owner.HasItem(ModContent.ItemType<PileOfConsequences>());

		if (distance < 16 * 3 || !hasItem)
			Projectile.Opacity = Math.Max(Projectile.Opacity - 0.1f, 0);
		else
			Projectile.Opacity = Math.Min(Projectile.Opacity + 0.1f, 1);

		if (!hasItem && Projectile.Opacity == 0)
			Projectile.Kill();
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(3, Main.projFrames[Type], 0, Projectile.frame, -2, -2);
		SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Color color = Projectile.GetAlpha(Color.Black) * 0.1f;

		int length = ProjectileID.Sets.TrailCacheLength[Type];

		for (int i = 0; i < length; i++)
			Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition, source, color, Projectile.oldRot[i], source.Size() / 2, Projectile.scale, effects);

		DrawBones(ref lightColor, effects);
		DrawHelpers.DrawChromaticAberration(Vector2.UnitX * 3, 1, (offset, color) =>
		{
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY) + offset;
			Main.EntitySpriteDraw(texture, position, source, Projectile.GetAlpha(color) * 0.1f, Projectile.rotation, source.Size() / 2, Projectile.scale, effects, 0);
		});

		return false;
	}

	private void DrawBones(ref Color lightColor, SpriteEffects effects)
	{
		Texture2D texture = DragonBoneParticle.Texture.Value;
		Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);
		SpriteEffects reverse = (effects == SpriteEffects.FlipHorizontally) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		float sine = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 80f);
		Rectangle source;

		DrawHelpers.DrawChromaticAberration(Vector2.UnitX * 3, 1, (offset, color) =>
		{
			Color alphaColor = Projectile.GetAlpha(color);

			Main.EntitySpriteDraw(texture, GetRelative(new(-4, 2 * sine)) + offset, source = GetFrame(2), alphaColor * 0.1f, Projectile.rotation, source.Size() / 2, Projectile.scale, effects);
			Main.EntitySpriteDraw(texture, GetRelative(new(9, -2 * sine)) + offset, source = GetFrame(3), alphaColor * 0.1f, Projectile.rotation, source.Size() / 2, Projectile.scale, reverse);
		});

		Rectangle GetFrame(int value) => texture.Frame(1, 4, 0, value, 0, -2);
		Vector2 GetRelative(Vector2 offset) => position + new Vector2(offset.X * Projectile.spriteDirection, offset.Y).RotatedBy(Projectile.rotation);
	}
}