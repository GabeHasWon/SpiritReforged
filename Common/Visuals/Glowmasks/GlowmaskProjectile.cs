namespace SpiritReforged.Common.Visuals.Glowmasks;

internal class GlowmaskProjectile : GlobalProjectile
{
	public static Dictionary<int, GlowmaskInfo> ProjIdToGlowmask = [];

	public override void PostDraw(Projectile projectile, Color lightColor)
	{
		if (ProjIdToGlowmask.TryGetValue(projectile.type, out var glow) && glow.DrawAutomatically)
		{
			var texture = glow.Glowmask.Value;
			var frame = texture.Frame(1, Main.projFrames[projectile.type]);

			Vector2 pos = projectile.Center - Main.screenPosition + new Vector2(0, projectile.gfxOffY);
			SpriteEffects effects = (projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			Main.EntitySpriteDraw(texture, pos, frame, glow.GetDrawColor(projectile), projectile.rotation, frame.Size() / 2f, projectile.scale, effects, 0);
		}
	}
}
