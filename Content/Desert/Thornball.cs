using SpiritReforged.Common.NPCCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert;

public class Thornball : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 99;
	public override void SetDefaults()
	{
		Item.DefaultToThrownWeapon(ModContent.ProjectileType<ThornballThrown>(), 20, 7, true);
		Item.noUseGraphic = true;
		Item.damage = 10;
		Item.knockBack = 2;
		Item.value = Item.sellPrice(copper: 20);
	}
}

public class ThornballThrown : ModProjectile
{
	public const int TimeLeftMax = 300;
	public override string Texture => ModContent.GetInstance<Thornball>().Texture;

	public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.SpikyBall);
		Projectile.Size = new Vector2(8);
		Projectile.hide = true;
		Projectile.penetrate = 5;
		Projectile.timeLeft = TimeLeftMax;
	}

	public override void AI()
	{
		const int fadeoutTime = 10;

		if (Projectile.timeLeft < fadeoutTime)
		{
			Projectile.scale -= 1f / fadeoutTime;
			Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, Alpha: 150, Scale: 2).noGravity = true;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.aiStyle != -1)
		{
			SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);

			Projectile.position += Projectile.velocity;
			Projectile.velocity = Vector2.Zero;

			Projectile.aiStyle = -1;
		}

		return false;
	}

	public override void OnKill(int timeLeft)
	{
		if (timeLeft > 0)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.OasisCactus).noGravity = true;
		}
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = (target.Center.X < Projectile.Center.X) ? -1 : 1;
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<Slowed>(), 180);

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), 
			null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, default);

		return false;
	}
	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);
}

/// <summary> Debuff that slows knockback-susceptible NPCs by 25% with no other effects. </summary>
public class Slowed : ModBuff
{
	public override string Texture => "Terraria/Images/Buff";
	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (npc.knockBackResist > 0)
			SlowdownGlobalNPC.ApplySlow(npc, 0.25f);
	}
}