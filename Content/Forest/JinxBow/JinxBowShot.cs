using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowShot : GlobalProjectile
{
	public const int TrailLength = 9;

	public bool IsJinxbowShot { get; set; } = false;
	public bool IsJinxbowSubshot { get; set; } = false;

	private readonly Vector2[] _oldPositions = new Vector2[TrailLength];

	public override bool InstancePerEntity => true;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		//Initialize old positions to projectile's center on spawn
		if(IsJinxbowShot)
		{
			//Trailing looks worse with high extra update arrows, convert it into velocity instead
			int maxExtraUpdates = Main.player[projectile.owner].magicQuiver ? 1 : 0;
			if(projectile.extraUpdates > maxExtraUpdates)
			{
				projectile.velocity *= projectile.extraUpdates / (float)maxExtraUpdates;
				projectile.extraUpdates = maxExtraUpdates;
			}

			for (int i = 0; i < _oldPositions.Length; i++)
				_oldPositions[i] = projectile.Center;
		}

		//If a jinxbow arrow spawns a projectile (i.e. Holy arrows, luminite arrows), the spawned projectile counts as a summon projectile instead of ranged.
		//Additionally applies to projectiles spawned from projectiles spawned by arrows, like holy arrow stars recursively spawning
		if (source is EntitySource_Parent { Entity: Projectile parent })
		{
			if (parent.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot || parent.GetGlobalProjectile<JinxBowShot>().IsJinxbowSubshot)
			{
				projectile.DamageType = DamageClass.Summon;
				IsJinxbowSubshot = true;
				projectile.netUpdate = true;
			}
		}
	}

	public override void PostAI(Projectile projectile)
	{
		if (!IsJinxbowShot)
			return;

		for(int i = TrailLength - 1; i > 0; i--)
			_oldPositions[i] = _oldPositions[i - 1];

		_oldPositions[0] = projectile.Center;
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (!IsJinxbowShot || Main.dedServ)
			return;

	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (!IsJinxbowShot)
			return true;

		//Partially adapted from hunter rifle vfx

		//Load texture if not already loaded
		Main.instance.LoadProjectile(873);

		var defaultTexture = TextureAssets.Projectile[projectile.type].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.Lavender);
		var brightest = TextureColorCache.GetBrightestColor(defaultTexture);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[873].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var color = (Color.Lerp(brightest.MultiplyRGBA(Color.Black * .5f), brightest, lerp) with { A = 0 }) * lerp;
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * projectile.scale;

			if (i == 0)
			{
				color = Color.White with { A = 200 };
				texture = defaultTexture;
				scale = new(projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 6; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 6f) * 2;
					Main.EntitySpriteDraw(solid, position + offset, null, color.Additive(200), projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
				}
			}
			else //Otherwise draw as trail
				Main.EntitySpriteDraw(solid, Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f), null, Color.Lerp(brightest, Color.Lavender, 0.5f).Additive() * EaseFunction.EaseCubicIn.Ease(lerp) * 0.5f, projectile.rotation, solid.Size() / 2, new Vector2(projectile.scale), SpriteEffects.None); 

			Main.EntitySpriteDraw(texture, position, null, color, projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(IsJinxbowShot);
		bitWriter.WriteBit(IsJinxbowSubshot);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		IsJinxbowShot = bitReader.ReadBit();
		IsJinxbowSubshot = bitReader.ReadBit();
	}
}