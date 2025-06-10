using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria;

namespace SpiritReforged.Content.Granite.Sharpshooter;

[AutoloadEquip(EquipType.HandsOn)]
[AutoloadGlowmask("255,255,255")]
[FromClassic("ShurikenLauncher")]
public class SharpshooterGlove : EquippableItem
{
	public const int EffectiveDistance = 480;

	public override void SetDefaults()
	{
		Item.width = Item.height = 38;
		Item.value = Item.buyPrice(gold: 2);
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
	}

	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
		=> Item.DrawInWorld(Color.White, rotation, scale, GlowmaskItem.ItemIdToGlowmask[Type].Glowmask.Value);
}

internal class SharpshooterPlayer : ModPlayer
{
	private static readonly Dictionary<int, float> CooldownByNPC = [];
	private static readonly Asset<Texture2D> Reticle = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(SharpshooterPlayer), "Reticle"));

	public float Stacks { get; private set; }

	public float GetDamageModifier(Projectile projectile, NPC.HitModifiers mods)
	{
		const float consecutiveBonusMax = 0.25f;
		float value = 1.2f;

		if (projectile.type != ProjectileID.ChlorophyteBullet)
		{
			Stacks = Math.Min(Stacks + MathHelper.Clamp(mods.SourceDamage.Base * 0.0005f, 0, 0.05f), consecutiveBonusMax);
		}

		return value + Stacks;
	}

	public static bool AtRange(Player player, NPC target) => player.Distance(target.Center) >= SharpshooterGlove.EffectiveDistance;
	private static void AddOrReplace(int index)
	{
		if (!CooldownByNPC.TryAdd(index, 1))
			CooldownByNPC[index] = 1;
	}

	public override void Load() => On_Main.DrawNPC += DrawReticle;

	private static void DrawReticle(On_Main.orig_DrawNPC orig, Main self, int iNPCIndex, bool behindTiles)
	{
		orig(self, iNPCIndex, behindTiles);

		if (Main.LocalPlayer.HasEquip<SharpshooterGlove>())
		{
			var npc = Main.npc[iNPCIndex];
			bool onCooldown = CooldownByNPC.TryGetValue(npc.whoAmI, out float value);

			if (AtRange(Main.LocalPlayer, npc))
				DrawSingle(Main.spriteBatch, npc, value);

			if (onCooldown && (CooldownByNPC[npc.whoAmI] -= 0.05f) <= 0)
				CooldownByNPC.Remove(iNPCIndex);
		}
	}

	private static void DrawSingle(SpriteBatch spriteBatch, NPC npc, float cooldown)
	{
		const float distanceFade = 500;

		float opacity = MathHelper.Clamp(1f - Main.MouseWorld.Distance(npc.Center) / distanceFade, 0, 1);
		if (opacity <= 0)
			return;

		var texture = Reticle.Value;
		float lerp = (float)Math.Sin(Main.timeForVisualEffects / 40f);
		float scale = 1 + lerp * 0.2f;

		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		spriteBatch.Draw(bloom, npc.Center - Main.screenPosition, null, (Color.Cyan * Math.Max(opacity, cooldown * 2) * 0.2f).Additive(), 0, bloom.Size() / 2, scale * 0.2f, default, 0);

		for (int i = 0; i < 5; i++)
		{
			int frame = i / 4;
			var source = texture.Frame(1, 2, 0, frame);
			float distance = lerp * 2f + cooldown * 8f;

			if (i == 4)
				distance = 0;

			float rotation = ((float)(Main.timeForVisualEffects / 40f % MathHelper.Pi) + MathHelper.PiOver2 * i) * (1f - frame);
			var position = npc.Center + (new Vector2(-1) * distance).RotatedBy(rotation) + new Vector2(0, npc.gfxOffY);

			DrawHelpers.DrawChromaticAberration(new Vector2(1), 1, delegate(Vector2 offset, Color color)
			{
				spriteBatch.Draw(texture, position - Main.screenPosition + offset, source, (color * Math.Max(opacity, cooldown) * 2).Additive(), rotation, source.Size() / 2, scale, default, 0);
			});
		}
	}

	public override void ResetEffects() => Stacks = Player.HasEquip<SharpshooterGlove>() ? Math.Max(Stacks - 0.0005f, 0) : 0;

	public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!Player.HasEquip<SharpshooterGlove>() || !proj.DamageType.CountsAsClass(DamageClass.Ranged) || !AtRange(Player, target))
			return;

		modifiers.FinalDamage *= GetDamageModifier(proj, modifiers);
		AddOrReplace(target.whoAmI);

		for (int i = 0; i < 6; i++)
		{
			var dust = Dust.NewDustPerfect(proj.Center, DustID.Electric, Scale: .5f);
			dust.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(.5f, 2f);
			dust.noGravity = true;
		}

		Vector2 position = target.Hitbox.ClosestPointInRect(proj.Center);
		ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, (Color.Cyan * 0.5f).Additive(), 1f, 100, 20, "supPerlin", Vector2.One, Common.Easing.EaseFunction.EaseQuinticOut).WithSkew(0.7f, proj.velocity.ToRotation()));

		Vector2 scale = new(0.5f, 2);
		ParticleHandler.SpawnParticle(new ImpactLine(position, Vector2.Normalize(proj.velocity) * 2, Color.Cyan.Additive(), scale, 10));
		ParticleHandler.SpawnParticle(new ImpactLine(position, Vector2.Normalize(proj.velocity) * 2, Color.White.Additive(), scale * 0.5f, 10));
	}
}