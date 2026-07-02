using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;
using static SpiritReforged.Content.Glyphs.Bee.BeeGlyph;

namespace SpiritReforged.Content.Glyphs.Bee;

public sealed class BeeGlyphPlayer : ModPlayer
{
	private static int[] _maxTimeLefts = new int[Main.maxCombatText];

	public override void Load() => On_CombatText.UpdateCombatText += FadeDamageText;

	private static void FadeDamageText(On_CombatText.orig_UpdateCombatText orig)
	{
		orig();

		for (int i = 0; i < Main.maxCombatText; i++)
		{
			CombatText text = Main.combatText[i];
			if (_maxTimeLefts[i] > 0)
				if (text.active)
				{
					Color blue, orange;

					blue = text.crit ? Color.Goldenrod : Color.Yellow;
					orange = text.crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

					text.color = Color.Lerp(blue, orange, EaseFunction.EaseCircularInOut.Ease(1f - text.lifeTime / (float)_maxTimeLefts[i]));
				}
				else
					_maxTimeLefts[i] = 0;
		}
	}

	public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			modifiers.HideCombatText();
	}

	public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (proj.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			modifiers.HideCombatText();
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			OnGlyphHit(target, hit, damageDone);
	}

	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (proj.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			OnGlyphHit(target, hit, damageDone);
	}

	public static void OnGlyphHit(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Color orange = hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

		CombatText.NewText(target.getRect(), orange, Math.Max((int)(damageDone * 0.8f), 1), hit.Crit);
		int summonDamage = CombatText.NewText(target.getRect(), Color.White, Math.Max((int)(damageDone * 0.2f), 1), hit.Crit);

		ColoredCombatText.AddCombatText(summonDamage, Color.Yellow, Color.Goldenrod);
	}
}

public class BeeGlobalNPC : GlobalNPC
{
	public const int MAX_TAG_COOLDOWN = 300;

	public override bool InstancePerEntity => true;

	public bool tagged;
	public int tagCooldown;
	private int _decayTimer;

	public bool CanExplode => tagged && tagCooldown <= 0;

	public override void ResetEffects(NPC npc)
	{
		if (tagCooldown > 0)
			tagCooldown--;

		if (_decayTimer > 0)
			_decayTimer--;
		else
			tagged = false;
	}

	public override void AI(NPC npc)
	{
		if (!Main.dedServ && tagged && Main.rand.NextBool(5) && ParticleHandler.Particles.Where(p => p is BeeOnNPC && (p as BeeOnNPC).Parent == npc).Count() < 3)
			ParticleHandler.SpawnParticle(new BeeOnNPC(npc, Main.rand.NextVector2Circular(25f, 25f)));

		if (Main.rand.NextBool(100) && tagged)
			ParticleHandler.SpawnParticle(new LargeBeeParticle(npc.Center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(2f, 2f), 0f, Main.rand.NextFloat(0.8f, 1.1f), 90 + Main.rand.Next(60)));
	}

	public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
	{
		if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
		{
			HitEffects(npc);

			if (!tagged)
				tagged = true;

			_decayTimer = 600;
		}
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		if (!projectile.TryGetOwner(out Player owner))
			return;

		if (projectile.IsMinionOrSentryRelated && CanExplode)
		{
			foreach (Particle p in ParticleHandler.Particles)
				if (p is BeeOnNPC && (p as BeeOnNPC).Parent == npc)
					p.Kill();

			TagEffects(owner, npc, damageDone);
			tagged = false;
			tagCooldown = MAX_TAG_COOLDOWN;
		}
		else if (!projectile.IsMinionOrSentryRelated && projectile.type is not ProjectileID.Bee or ProjectileID.GiantBee && projectile.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
		{
			HitEffects(npc);
			if (!tagged)
				tagged = true;

			_decayTimer = 600;
		}
	}

	private static void HitEffects(NPC target)
	{
		Vector2 position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position, DustID.Honey2, Main.rand.NextVector2Circular(2f, 2f), 50, default, 1.2f).noGravity = true;

			Vector2 pos = position + Main.rand.NextVector2CircularEdge(9f, 9f);

			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Orange, 0.2f, 20, 0));
			ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.White * 0.5f, 0.1f, 20, 0));
		}

		if (Main.rand.NextBool(5))
			ParticleHandler.SpawnParticle(new BeeOnNPC(target, Main.rand.NextVector2Circular(25f, 25f)));
	}

	private static void TagEffects(Player player, NPC target, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.Item97 with { Volume = 1f, PitchVariance = 0.25f }, target.Center);

		for (int i = 0; i < 7; i++)
		{
			Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), DustID.Bee, Main.rand.NextVector2Circular(5f, 5f), 50, default, 1.2f).noGravity = true;
			Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), DustID.Honey, Main.rand.NextVector2Circular(5f, 5f), 50, default, 1.2f).noGravity = true;

			ParticleHandler.SpawnParticle(new StickyHoneyParticle(target.Center + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(5f, 5f), 1f, 90, 0.15f));
			ParticleHandler.SpawnParticle(new StickyHoneyParticle(target.Center + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(8f, 8f), 1f, 30, 0.15f));
		}

		int type = player.beeType();

		for (int i = 0; i < 3; i++)
			Projectile.NewProjectile(target.GetSource_OnHurt(player), target.Center, Main.rand.NextVector2Unit(), type, player.beeDamage(1 + damageDone / 3), player.beeKB(2f), player.whoAmI); // Make into a tag bonus
	}
}