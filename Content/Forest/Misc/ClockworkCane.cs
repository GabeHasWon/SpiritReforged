using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Misc;

public class ClockworkCane : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<ClockworkCog>(), 25, 0, false);
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.DamageType = DamageClass.Summon;
		Item.mana = 20;
		Item.damage = 14;
		Item.knockBack = 2.5f;
		Item.UseSound = SoundID.Item44;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => position = Main.MouseWorld;
}

public class ClockworkCog : BaseMinion, IDrawPixelated
{
	private VertexTrail _trail;

	public ClockworkCog() : base(600, 800, new Vector2(28)) { }

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		Main.projFrames[Type] = 6;
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
	}

	public override bool PreAI()
	{
		if (!Main.dedServ)
		{
			CreateTrail();
			_trail.Update();
		}

		Vector2 result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(Main.MouseWorld), 0.1f);
		if (!result.HasNaNs())
			Projectile.velocity = result; //DEBUG

		Projectile.rotation += 0.05f;

		return true;
	}

	public override void IdleMovement(Player player)
	{

	}

	public override void TargettingBehavior(Player player, NPC target)
	{

	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		//if (Main.player[Projectile.owner]) //Has wrench
		{
			ItemMethods.NewItemSynced(Projectile.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);
		}
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe) => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(2, Main.projFrames[Type], 0, Projectile.frame, -2, -2);
		Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

		Main.EntitySpriteDraw(texture, center, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

		source = texture.Frame(2, Main.projFrames[Type], 1, Projectile.frame, -2, -2);
		float eyeRotation = (_targetNPC is NPC target) ? Projectile.AngleTo(target.Center) : Projectile.velocity.ToRotation();
		Main.EntitySpriteDraw(texture, center, source, Projectile.GetAlpha(lightColor), eyeRotation, source.Size() / 2, Projectile.scale, 0);

		return false;
	}

	void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.graphics.GraphicsDevice, Matrix.Identity);

	private void CreateTrail()
	{
		ITrailCap cap = new RoundCap();
		ITrailPosition position = new EntityTrailPosition(Projectile);
		ITrailShader shader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trail ??= new VertexTrail(new GradientTrail(Color.Transparent, Color.RosyBrown.Additive(50), EaseFunction.EaseQuarticInOut), cap, position, shader, 30, 60);
	}
}