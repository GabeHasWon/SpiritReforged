using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Forest.Glyphs;

public class BeeGlyph : GlyphItem
{
	public class BeeInOrbit : Particle
	{
		public NPC Parent => Main.npc[_parentWhoAmI];

		public override ParticleDrawType DrawType => ParticleDrawType.Custom;

		public bool drawBehind;
		private float _rotationOffset;
		private readonly int _parentWhoAmI;
		private readonly float _animationSpeed;

		public override ParticleLayer DrawLayer => drawBehind ? ParticleLayer.BelowSolid : ParticleLayer.AboveNPC;

		public BeeInOrbit(NPC npc, float speed)
		{
			_parentWhoAmI = npc.whoAmI;
			_animationSpeed = speed;

			MaxTime = 60 * 5;
			Scale = 1f;
		}

		public override void Update()
		{
			_rotationOffset += Main.rand.NextFloat(0.05f);

			float rate = TimeActive * _animationSpeed;
			float sin = (float)Math.Sin(rate);
			float cos = (float)Math.Cos(rate);

			Position = Parent.Center + new Vector2(Parent.width * cos, 0f).RotatedBy(_rotationOffset);
			Rotation = MathHelper.Lerp(Rotation, cos, 0.05f);

			if (sin is < 1f and > (-0.5f))
				drawBehind = true;
			else
				drawBehind = false;
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			const int type = ProjectileID.Bee;

			Texture2D texture = TextureAssets.Projectile[type].Value;
			Rectangle source = texture.Frame(1, Main.projFrames[type], 0, (int)(TimeActive / 4 % Main.projFrames[type]), 0, 0);
			Color color = Lighting.GetColor(Position.ToTileCoordinates());
			SpriteEffects effects = (Position.X < Parent.Center.X) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			if (drawBehind)
				color = color.MultiplyRGB(Color.White * 0.75f);

			spriteBatch.Draw(texture, Position - Main.screenPosition, source, color, Rotation, source.Size() / 2, 1, effects, 0);
		}
	}

	public sealed class BeeNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool tagged;

		public override void AI(NPC npc)
		{
			if (!Main.dedServ && tagged && Main.rand.NextBool(30))
				ParticleHandler.SpawnParticle(new BeeInOrbit(npc, 0.1f));
		}

		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
				tagged = true;
		}

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			if (!projectile.TryGetOwner(out Player owner))
				return;

			if (tagged && projectile.IsMinionOrSentryRelated)
			{
				TagEffects(owner, npc);
				tagged = false;
			}
			else if (!projectile.IsMinionOrSentryRelated && projectile.type is not ProjectileID.Bee or ProjectileID.GiantBee && projectile.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			{
				tagged = true;
			}
		}

		private static void TagEffects(Player player, NPC target)
		{
			int type = player.hornet ? ProjectileID.GiantBee : ProjectileID.Bee;

			for (int i = 0; i < 3; i++)
				Projectile.NewProjectile(target.GetSource_OnHurt(player), target.Center, Main.rand.NextVector2Unit(), type, 10, 0, player.whoAmI); //Make into a tag bonus
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Goldenrod);
	}

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !item.DamageType.CountsAsClass(DamageClass.Summon);

	public override void ApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Summon, 0.2f));

		base.ApplyGlyph(item, context);
	}
}