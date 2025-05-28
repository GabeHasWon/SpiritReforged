using SpiritReforged.Common.NPCCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Snow;

public class ElephantKite : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.IsAKite[Type] = true;
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.ZombieEskimo), ItemDropRule.Common(Type, 35)));
	}

	public override void SetDefaults() => Item.DefaultTokite(ModContent.ProjectileType<ElephantKiteProj>());
	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
}

internal class ElephantKiteProj : ModProjectile
{
	public override LocalizedText DisplayName => ModContent.GetInstance<ElephantKite>().DisplayName;

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.KiteBlue);
		Projectile.scale = 0;
	}

	public override void AI()
	{
		if (Projectile.extraUpdates == 0)
			Projectile.scale = Math.Min(Projectile.scale + 0.1f, 1);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		DrawString();

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		SpriteEffects effects = (Projectile.spriteDirection != 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		return false;
	}

	private void DrawString()
	{
		Texture2D line = TextureAssets.FishingLine.Value;
		Vector2 armPosition = Main.GetPlayerArmPosition(Projectile);
		Vector2 position = armPosition;

		int count = (int)(armPosition.Distance(Projectile.Center) / (line.Height - 1));
		float fullAngle = 0.2f * Math.Clamp(-Projectile.velocity.Y, -1f, 1f) * Main.player[Projectile.owner].direction;

		for (int i = 1; i < count; i++)
		{
			float progress = (float)i / count;
			float finalAngle = MathHelper.Lerp(fullAngle, -fullAngle, progress);

			float rotation = armPosition.AngleTo(Projectile.Center) + finalAngle - MathHelper.PiOver2;
			position += new Vector2(0, line.Height - 1).RotatedBy(rotation);

			Color color = Lighting.GetColor(position.ToTileCoordinates());

			Main.EntitySpriteDraw(line, position - Main.screenPosition, null, color, rotation, line.Frame().Bottom(), 1, default, 0);
		}
	}
}