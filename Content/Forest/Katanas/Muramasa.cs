using Mono.Cecil;
using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.VerletChains;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas;

public class Muramasa : GlobalItem, IDrawHeld
{
	public sealed class MuramasaSwing : SwungProjectile, IDrawPixelated
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

		public override LocalizedText DisplayName => Lang.GetItemName(ItemID.Muramasa);

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuarticOut, 84, 25);

		public override void AI()
		{
			if (Secondary)
			{

			}
			else
			{
				base.AI();
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch) => base.GetRotation(out armRotation, out stretch) + MathHelper.PiOver4 * SwingDirection;

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			DrawHeld(lightColor, new Vector2(0, (effects == SpriteEffects.FlipVertically) ? 0 : TextureAssets.Projectile[Type].Value.Height), Projectile.rotation, effects);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			if (SwingArc != 0)
			{
				Player owner = Main.player[Projectile.owner];

				//Draw a custom smear
				Main.instance.LoadProjectile(985);
				Texture2D smear = TextureAssets.Projectile[985].Value;

				SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
				Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 14f));
				float rotation = Projectile.rotation + SwingDirection * Progress - MathHelper.PiOver4 * SwingDirection;

				Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
				Vector2 origin = new(source.Width, source.Height / 2);
				Vector2 smearWorldPosition = owner.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
				Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

				IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

				spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.Blue)), rotation, origin, 0.45f, effects, 0);

				source = smear.Frame(1, 4, 0, (int)(Progress * 20f));
				spriteBatch.Draw(smear, smearDrawPosition, source, Projectile.GetAlpha(lightColor.MultiplyRGB(Color.DodgerBlue)), rotation, origin, 0.45f, effects, 0);
			}
		}
	}

	public override bool InstancePerEntity => true;

	public static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<Muramasa>("Muramasa_Held", false);
	private float _swingArc;
	private float _freeRotation;

	#region chain
	private static readonly Rectangle _chainSource = new(12, 22, 6, 12);
	private static readonly Rectangle _lockSource = new(0, 20, 10, 14);

	public Chain GetChain(Player player)
	{
		if (_chain == null)
		{
			int length = _chainSource.Height;
			return _chain = new Chain(length - 2, 5, player.Center, new ChainPhysics(0.9f, 0.5f, 3f));
		}
		else
		{
			return _chain;
		}
	}

	private Chain _chain;
	private Vector2 _endPosition;

	private void ApplyChainPhysics(Player player, Vector2 end)
	{
		if (_endPosition == default)
			_endPosition = end;

		Chain chain = GetChain(player);
		Vector2 startPosition = end;
		Vector2 endPosition = _endPosition;

		endPosition += new Vector2(0, 3f); //Gravity
		endPosition -= ChainConstraint(startPosition, _endPosition, (int)(chain.Segments[0].Length * chain.Segments.Count));

		if (!endPosition.HasNaNs())
			_endPosition = endPosition;

		chain.Update(startPosition, endPosition);
	}

	private static Vector2 ChainConstraint(Vector2 start, Vector2 end, int totalLength)
	{
		Vector2 delta = start - end;
		float distance = delta.Length();
		float finalDistance = totalLength - distance;

		if (finalDistance > 0) //Compact indefinitely
		{
			return Vector2.Zero;
		}

		float fraction = finalDistance / Math.Max(distance, 1) / 2;
		delta *= fraction;

		return delta;
	}
	#endregion

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.Muramasa;

	public override void SetStaticDefaults() => SpiritSets.IsSword[ItemID.Muramasa] = SpiritSets.IsKatana[ItemID.Muramasa] = true;

	public override void SetDefaults(Item entity)
	{
		int animationTime = entity.useAnimation;
		entity.DefaultToSpear(ModContent.ProjectileType<MuramasaSwing>(), 1, animationTime);
	}

	public override void HoldItem(Item item, Player player)
	{
		if (!player.ItemAnimationActive)
		{
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, -0.35f * player.direction);
			_freeRotation = MathHelper.Lerp(_freeRotation, player.velocity.X / 5f, 0.2f);
		}

		if (!Main.dedServ)
			ApplyChainPhysics(player, player.GetFrontHandPosition(0, player.compositeFrontArm.rotation));
	}

	public override bool AltFunctionUse(Item item, Player player) => true;

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			3f => 5f,
			5f => -5f,
			_ => 3f
		};

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);
		return false;
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Vector2 lockOrigin = new(_lockSource.Width / 2, 0);
		if (!drawinfo.drawPlayer.ItemAnimationActive)
		{
			Texture2D texture = HeldTexture.Value;
			IDrawHeld.DrawSwordHeld(ref drawinfo, texture, texture.Frame(1, 2));

			Vector2 bobOffset = Main.OffsetsPlayerHeadgear[drawinfo.drawPlayer.bodyFrame.Y / drawinfo.drawPlayer.bodyFrame.Height] * drawinfo.drawPlayer.gravDir;
			Vector2 center = drawinfo.drawPlayer.MountedCenter + bobOffset;
			Vector2 drawPos = new((int)(center.X + 20 * drawinfo.drawPlayer.direction - Main.screenPosition.X), (int)(center.Y + 4 * drawinfo.drawPlayer.gravDir - Main.screenPosition.Y + drawinfo.drawPlayer.gfxOffY));

			float rotation = _freeRotation;
			Color color = Lighting.GetColor((int)drawinfo.drawPlayer.Center.X / 16, (int)drawinfo.drawPlayer.Center.Y / 16);

			drawinfo.DrawDataCache.Add(new DrawData(texture, drawPos, _lockSource, color, rotation, lockOrigin, 1, drawinfo.playerEffect, 0));
		}
		else
		{
			Chain chain = GetChain(drawinfo.drawPlayer);
			chain.Draw(Main.spriteBatch, HeldTexture.Value, _chainSource);
			Color lightColor = Lighting.GetColor(chain.EndPosition.ToTileCoordinates());

			Main.spriteBatch.Draw(HeldTexture.Value, chain.EndPosition - Main.screenPosition, _lockSource, lightColor, chain.EndRotation + MathHelper.PiOver2, lockOrigin, 1, 0, 0);
		}
	}
}