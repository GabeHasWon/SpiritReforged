using System.Runtime.CompilerServices;

namespace SpiritReforged.Content.Savanna.DustStorm;

//Behaviourally similar to graveyard fog gores
public class SavannaCloud : ModDust
{
	public override string Texture => "Terraria/Images/Gore_1087";

	public static ref float FadeSpeed(Dust dust) => ref Unsafe.Unbox<float>(dust.customData);

	public override void Load() => On_Main.DrawBackgroundBlackFill += DrawUnderTiles;

	//Draw the dusts again under tiles for an interesting depth effect and higher density
	private void DrawUnderTiles(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
	{
		if (!Main.gameMenu) //Don't draw our dusts on the main menu
			foreach (var dust in Main.dust)
			{
				if (!dust.active || dust.type != Type)
					continue;

				Draw(dust, true);
			}

		orig(self);
	}

	public override void OnSpawn(Dust dust)
	{
		if (!Main.dedServ)
		{
			//Ensure all of the gore textures we need are loaded
			Main.instance.LoadGore(GoreID.AmbientFloorCloud1);
			Main.instance.LoadGore(GoreID.AmbientFloorCloud2);
			Main.instance.LoadGore(GoreID.AmbientFloorCloud3);
			Main.instance.LoadGore(GoreID.AmbientFloorCloud4);
		}

		dust.customData = 1f;
	}

	public override bool Update(Dust dust)
	{
		bool CanFadeOut() => dust.fadeIn == 1;

		float fadeOutTime = 255 * FadeSpeed(dust); //The amount of time, in ticks, the dust will take to fade out completely
		int fadeInTime = 20; //The amount of time, in ticks, the dust will take to fade in on spawn

		if (CanFadeOut())
		{
			dust.alpha = Math.Min(dust.alpha + (int)(255f / fadeOutTime), 255); //Fade out when finished fading in
			dust.scale += .005f;
			dust.velocity.X *= .99f;
			dust.velocity.Y -= .0005f;
		}
		else
			dust.fadeIn = Math.Min(dust.fadeIn + 1f / fadeInTime, 1);

		dust.position += dust.velocity;

		if (WorldGen.SolidTile(Framing.GetTileSafely(dust.position))) //In contact with a solid tile
		{
			if (CanFadeOut())
				dust.alpha++; //Fade out faster

			dust.velocity.X *= .95f; //Quickly slow down
			dust.rotation -= Main.rand.NextFloat(.0005f, .004f) * Math.Sign(dust.velocity.X);
			dust.velocity.Y -= .0005f;
		}

		if (dust.alpha >= 255)
			dust.active = false; //Die when fully transparent

		return false;
	}

	public override bool PreDraw(Dust dust)
	{
		Draw(dust);
		return false;
	}

	private static void Draw(Dust dust, bool behind = false)
	{
		var texture = (dust.dustIndex % 4) switch
		{
			1 => TextureAssets.Gore[GoreID.AmbientFloorCloud1],
			2 => TextureAssets.Gore[GoreID.AmbientFloorCloud2],
			3 => TextureAssets.Gore[GoreID.AmbientFloorCloud3],
			_ => TextureAssets.Gore[GoreID.AmbientFloorCloud4],
		};

		if (behind)
		{
			float opacity = .113f; //Forced maximum opacity
			var color = dust.GetAlpha(Lighting.GetColor((dust.position / 16).ToPoint()).MultiplyRGB(dust.color * .75f)) * dust.fadeIn * opacity;
			var offset = new Vector2(-80, 8);

			Main.EntitySpriteDraw(texture.Value, dust.position + offset - Main.screenPosition, null, color, dust.rotation, texture.Size() / 2, dust.scale * 2, SpriteEffects.None, 0);
		}
		else
		{
			float opacity = .16f; //Forced maximum opacity
			var color = dust.GetAlpha(Lighting.GetColor((dust.position / 16).ToPoint()).MultiplyRGB(dust.color)) * dust.fadeIn * opacity;

			Main.EntitySpriteDraw(texture.Value, dust.position - Main.screenPosition, null, color, dust.rotation, texture.Size() / 2, dust.scale, SpriteEffects.None, 0);
		}
	}
}

public class SavannaSand : ModDust
{
	public override string Texture => "Terraria/Images/Dust";

	public override void OnSpawn(Dust dust) => dust.frame = Texture2D.Frame(100, 12, DustID.Sand, Main.rand.Next(3), -2, -2);

	public override bool Update(Dust dust)
	{
		dust.position += dust.velocity;

		if (dust.customData is float curve)
		{
			dust.customData = curve * (curve + 1);
			dust.velocity = dust.velocity.RotatedBy(-MathHelper.Min(curve, .45f) * Math.Sin(Main.windSpeedCurrent));
		}

		dust.alpha++;
		dust.scale -= .01f;

		if (dust.scale <= 0 || dust.alpha >= 255 || WorldGen.SolidTile(Framing.GetTileSafely(dust.position)))
			dust.active = false;

		return false;
	}
}
