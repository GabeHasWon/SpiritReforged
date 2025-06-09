using SpiritReforged.Common.ModCompat;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.CactusStaff;

public class CactusWallProj : ModProjectile
{
	public ref float FadeInCounter => ref Projectile.ai[0];

	/// <summary> The maximum number of hits that can be sustained before dying, counted with <see cref="Projectile.numHits"/>. </summary>
	private const int MaxHits = 10;
	private const int MaxTimeLeft = 15 * 60;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
		Main.projFrames[Type] = 3;

		MoRHelper.AddElement(Projectile, MoRHelper.Nature);
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 72;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.timeLeft = MaxTimeLeft;
		Projectile.penetrate = -1;
		Projectile.frame = Main.rand.Next(3);
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.hide = true; //Used in conjunction with DrawBehind

		DrawOriginOffsetY = -30;
	}

	public override void OnSpawn(IEntitySource source)
	{
		Tile tile = Main.tile[Projectile.Bottom.ToTileCoordinates()];

		if (tile.TopSlope)
			Projectile.position.Y += 16;
		else if (tile.IsHalfBlock)
			Projectile.position.Y += 8;

		for (int j = 0; j < 3; ++j) //This is for tripling per offset
			Dust.NewDust(Projectile.BottomLeft, 16, 1, DustID.t_Cactus);
	}

	public override void AI()
	{
		if (Projectile.timeLeft > MaxTimeLeft - 25)
		{
			FadeInCounter++;
			Projectile.Opacity = MathHelper.Lerp(0f, 1f, (float)FadeInCounter / 20);
		}

		Tile tile = Framing.GetTileSafely(Projectile.Bottom.ToTileCoordinates());
		if (!WorldGen.SolidOrSlopedTile(tile) && !Main.tileSolidTop[tile.TileType])
			Projectile.Kill();
		//If no solid tiles exist below the projectile, kill it

		if (Projectile.timeLeft < 20)
			Projectile.Opacity = Projectile.timeLeft / 10f;

		Projectile.rotation = MathF.Sin((Projectile.timeLeft + Projectile.whoAmI) * 0.05f) * 0.05f;
	}

	public override void OnKill(int timeLeft)
	{
		if (timeLeft <= 0)
			return;

		for (int i = 0; i < 4; ++i) //This is for offsetting position
			for (int j = 0; j < 3; ++j) //This is for tripling per offset
				Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + i * 16), 16, 16, DustID.t_Cactus);

		if (Main.netMode != NetmodeID.Server)
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.NPCHit7 with { Volume = .65f }, Projectile.Center);
		SoundEngine.PlaySound(SoundID.NPCHit11 with { Volume = .4f }, Projectile.Center);

		if (Projectile.numHits >= MaxHits)
		{
			Projectile.netUpdate = true;
			Projectile.Kill();
		}
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		=> modifiers.HitDirectionOverride = target.Center.X < Projectile.Center.X ? -1 : 1;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		=> behindNPCsAndTiles.Add(index);

	public override bool PreDraw(ref Color lightColor)
	{
		var tex = TextureAssets.Projectile[Type].Value;
		int frameHeight = tex.Height / Main.projFrames[Type];
		var src = new Rectangle(0, frameHeight * Projectile.frame, Projectile.width, frameHeight);
		var pos = Projectile.position - Main.screenPosition + new Vector2(0, frameHeight - 4);

		float lifespan = MathHelper.Clamp(FadeInCounter / 25, 0f, 1f);
		float squash = MathHelper.Lerp(.35f, 0f, lifespan);
		float stretch = MathHelper.Lerp(.5f, 0f, lifespan);
		Vector2 scale = new Vector2(1 + squash, 1 - stretch) * Projectile.scale;

		const int SpawnAnimSpeed = 10;

		if (Projectile.timeLeft - MaxTimeLeft > -SpawnAnimSpeed)
		{
			src.Height = (int)(Math.Abs(Projectile.timeLeft - MaxTimeLeft) / (float)SpawnAnimSpeed * frameHeight);
			pos.Y += frameHeight - src.Height;
		}

		Main.EntitySpriteDraw(tex, pos, src, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(0, frameHeight), scale, SpriteEffects.None, 0);
		return false;
	}
}
