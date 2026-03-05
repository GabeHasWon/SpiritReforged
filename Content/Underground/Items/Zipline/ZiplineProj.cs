using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.Zipline;

public class ZiplineProj : ModProjectile
{
	public Vector2 cursorPoint;

	public override void SetDefaults()
	{
		Projectile.extraUpdates = 3;
		Projectile.tileCollide = false;
		Projectile.alpha = 255;
	}

	public override void AI()
	{
		Projectile.Opacity = MathHelper.Min(Projectile.Opacity + .025f, 1);
		Projectile.rotation = Projectile.velocity.ToRotation();

		float length = Projectile.velocity.Length();
		if (Projectile.Distance(cursorPoint) <= length + 8)
		{
			Projectile.Center = cursorPoint;
			Projectile.Kill();
		}
	}

	public override void OnKill(int timeLeft)
	{
		if (timeLeft == 0)
			return; //Don't interact with rails after timing out for whatever reason

		bool removed = false;
		foreach (var zipline in ZiplineHandler.Ziplines)
		{
			if (zipline.Owner == Main.player[Projectile.owner])
				UpdateExisting(zipline, out removed);
		}

		if (!removed)
			ZiplineHandler.Add(Main.player[Projectile.owner], Projectile.Center);

		DeathEffects(Projectile.Center);
	}

	private void UpdateExisting(Zipline zipline, out bool removed)
	{
		removed = false;

		if (zipline.Contains(Projectile.Center.ToPoint(), out var contained))
		{
			zipline.RemovePoint(contained);
			removed = true;
		}
		else
		{
			var last = zipline.points.Last();

			if ((last / 16).Distance(Main.MouseWorld / 16) > ZiplineGun.ExceedDist + .5f)
				ZiplineHandler.Ziplines.Remove(zipline); //Remove the last rail if distance is excessive
		}
	}

	public static void DeathEffects(Vector2 position)
	{
		if (Main.dedServ)
			return;

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, (Color.Goldenrod * .75f).Additive(), 1, 100, 30, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut));
		ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, (Color.White * .5f).Additive(), 1, 100, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut));

		for (int i = 0; i < 12; i++)
			Dust.NewDustPerfect(position, DustID.AmberBolt, Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f), Scale: Main.rand.NextFloat(.5f, 1.5f)).noGravity = true;

		SoundEngine.PlaySound(SoundID.Item101 with { Pitch = .25f }, position);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var trail = AssetLoader.LoadedTextures["Ray"].Value;
		var scale = new Vector2(.45f, 1f) * Projectile.scale;

		Main.EntitySpriteDraw(trail, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.Orange.Additive()), 
			Projectile.rotation + MathHelper.PiOver2, new Vector2(trail.Width / 2, 0), scale, default);

		Main.EntitySpriteDraw(trail, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White.Additive()),
			Projectile.rotation + MathHelper.PiOver2, new Vector2(trail.Width / 2, 0), scale * .75f, default);

		Projectile.QuickDraw();

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(cursorPoint);
	public override void ReceiveExtraAI(BinaryReader reader) => cursorPoint = reader.ReadVector2();
}
