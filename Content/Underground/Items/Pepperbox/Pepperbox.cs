using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Survivalist;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Underground.Items.Pepperbox;

public class Pepperbox : ReloadableItem
{
	private class PepperboxFlash : Particle
	{
		public override string TexturePath => AssetLoader.EmptyTexture;

		private readonly int _variant;

		public PepperboxFlash(Vector2 position, Vector2 velocity, float scale, int maxTime)
		{
			Position = position;
			Velocity = velocity;
			Scale = scale;
			MaxTime = maxTime;

			Color = Color.White;

			_variant = Main.rand.Next(3);
		}

		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);
			Velocity *= 0.95f;
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
		{
			var texture = Texture;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			var frame = texture.Frame(1, 3, 0, _variant);

			float progress = EaseFunction.EaseCircularOut.Ease(1f - Progress);

			if (progress > 0.9f)
			{
				float lerp = (progress - 0.9f) / 0.1f;
				spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color * lerp, 0f, frame.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - lerp), 0f, 0f);
			}

			spriteBatch.Draw(bloom, Position - Main.screenPosition, null, new Color(255, 150, 0, 0) * progress * 0.25f, 0f, bloom.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - progress) * 0.33f, 0f, 0f);
			spriteBatch.Draw(bloom, Position - Main.screenPosition, null, new Color(255, 180, 0, 0) * progress * 0.2f, 0f, bloom.Size() / 2f, MathHelper.Lerp(Scale, Scale * 0.75f, 1f - progress) * 0.25f, 0f, 0f);
		}
	}

	public static readonly Asset<Texture2D> Handle = DrawHelpers.RequestLocal<Pepperbox>("Pepperbox_Handle", false);
	public static readonly Asset<Texture2D> Barrel = DrawHelpers.RequestLocal<Pepperbox>("Pepperbox_Barrel", false);

	public override IReloadable.MagazineInfo Magazine => new(3, 90);

	public override InterfaceProfile? Profile => new(MagazinePlayer.StyleID.Shell);

	private bool _spawnedReloadDust = false;

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedEyeOfCthulhu)));

	public override void SetDefaults()
	{
		Item.DefaultToShotgun(25, 1);
		Item.damage = 4;
		Item.knockBack = 6;
		Item.width = 40;
		Item.height = 20;
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item36;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		StatModifiers modifiers = new(
			StatModifier.Default,
			StatModifier.Default);

		ShotgunGlobalItem.ShotgunShoot(modifiers, source, position, velocity, type, damage, knockback);

		Vector2 shellPosition = position + new Vector2(12, -10 * player.direction).RotatedBy(velocity.ToRotation());
		Vector2 muzzlePosition = position + new Vector2(56, -2 * player.direction).RotatedBy(velocity.ToRotation());

		if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ModItem is IShotgunAmmo shotgunAmmo)
			ParticleRenderers.OverPlayers.Add(new ShotgunShellParticle(shellPosition, -velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(1f, 4f) - Vector2.UnitY * Main.rand.NextFloat(3f), 1f, 120, shotgunAmmo));
		
		ParticleRenderers.OverPlayers.Add(new PepperboxFlash(muzzlePosition, velocity, Main.rand.NextFloat(0.9f, 1.1f), 20 + Main.rand.Next(5, 10)));

		for (int i = 0; i < 3; i++)
		{
			ParticleRenderers.OverPlayers.Add(new CompositeSmoke(muzzlePosition + Main.rand.NextVector2Circular(5f, 5f), velocity, Color.White * 0.4f, 75, default, (particle) => particle.Velocity.Y -= 0.01f));
			ParticleRenderers.OverPlayers.Add(new EmberParticle(muzzlePosition, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(3f), new Color(200, 160, 0, 0), 0.2f, 30, 2));
		}

		_spawnedReloadDust = false;
		return false;
	}

	public override void ItemStyle(Player player, Rectangle heldItemFrame, bool inUse)
	{
		if (!ReloadInfo.IsReloading(player))
			return;

		float animProgress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;
		float itemRotation = player.compositeBackArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < 0.15f)
		{
			float lerper = animProgress / 0.15f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-4f, -8f, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			if (animProgress < 0.75f)
			{
				itemPosition += itemRotation.ToRotationVector2() * -8f;
			}
			else
			{
				float lerper = (animProgress - 0.75f) / 0.25f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-8f, -2f, EaseFunction.EaseCircularInOut.Ease(lerper));
			}
		}

		if (animProgress > 0.2f && !_spawnedReloadDust)
		{
			Vector2 breakPosition = itemPosition + new Vector2(12f, -8f).RotatedBy(itemRotation);
			Vector2 breakVelocity = -Vector2.UnitY - Vector2.UnitX * player.direction;

			for (int i = 0; i < 2; i++)
			{
				Dust.NewDustPerfect(breakPosition + Main.rand.NextVector2Circular(15, 15), DustID.Torch, breakVelocity + Main.rand.NextVector2Circular(0.5f, 0.5f), 0, default, Main.rand.NextFloat(1.5f)).noGravity = true;

				ParticleRenderers.OverPlayers.Add(new CompositeSmoke(breakPosition + Main.rand.NextVector2Circular(5, 5), breakVelocity + Main.rand.NextVector2Circular(0.25f, 0.25f), Color.AntiqueWhite * 0.25f, 45)
				{ Variant = CompositeSmoke.SmallVariant });
			}

			SoundEngine.PlaySound(Survivalist.BreakOpen, breakPosition);
			_spawnedReloadDust = true;
		}
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(2, 18);

		if (ReloadInfo.IsReloading(drawInfo.drawPlayer))
		{
			float progress = 1f - ReloadInfo.reloadTime / (float)Magazine.ReloadDuration;

			Texture2D handle = Handle.Value;
			Texture2D barrel = Barrel.Value;
			float maxRotation = 0.9f * drawInfo.drawPlayer.direction;
			float barrelRot = drawData.rotation;

			if (progress is > 0.1f and < 0.4f)
			{
				float lerp = EaseFunction.EaseCircularOut.Ease((progress - 0.1f) / 0.3f);
				barrelRot += MathHelper.Lerp(0, maxRotation, lerp);
			}
			else if (progress is >= 0.4f and < 0.7f)
			{
				barrelRot += maxRotation;
			}
			else if (progress >= 0.7f)
			{
				float lerp = EaseFunction.EaseInOutBack().Ease((progress - 0.7f) / 0.3f);
				barrelRot += MathHelper.Lerp(maxRotation, 0, lerp);
			}

			Vector2 offset = new Vector2(18 * player.direction, -7).RotatedBy(drawData.rotation);

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = handle }, player, origin, out float armRotation));
			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { texture = barrel, rotation = barrelRot }, player, barrel.Size() / 2, out _, offset: offset));

			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

			return false;
		}
		else if (player.ItemAnimationActive)
		{
			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, player, origin, out float armRotation, new Recoil(EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]), 3, 1), new Vector2(10 * Math.Min(progress * 4, 1) - 10, 0).RotatedBy(drawData.rotation)));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		}

		return false;
	}
}