using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Safekeeper;

//[AutoloadEquip(EquipType.HandsOn)]
public class SafekeeperRing : EquippableItem
{
	public override void SetStaticDefaults() => DiscoveryHelper.RegisterPickup(Type, SoundID.CoinPickup with { Pitch = .25f });

	public override void SetDefaults()
	{
		Item.width = Item.height = 12;
		Item.value = Item.buyPrice(gold: 2, silver: 50);
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		float mult = 0;
		foreach (var npc in Main.ActiveNPCs)
		{
			if (UndeadNPC.IsUndeadType(npc.type))
				mult = MathHelper.Max(mult, 1f - npc.Distance(player.Center) / 1200f);
		}

		if (mult > 0)
			EmitLight(player, mult, hideVisual);
	}

	private static void EmitLight(Player player, float strength, bool hideVisual)
	{
		var color = Color.LightGoldenrodYellow.ToVector3() * .34f * strength;
		Lighting.AddLight(player.Center, color.X, color.Y, color.Z);

		if (hideVisual || strength <= .8f)
			return;

		if (Main.rand.NextBool(30))
		{
			var position = Main.rand.NextVector2FromRectangle(player.getRect());
			var newCol = Color.Orange * .5f;
			var vel = Vector2.UnitY * -Main.rand.NextFloat(.5f, 1f);
			float scale = Main.rand.NextFloat(.2f, .4f);

			ParticleHandler.SpawnParticle(new GlowParticle(position, vel, newCol, scale, 60, 20));
			ParticleHandler.SpawnParticle(new GlowParticle(position, vel, Color.White, scale * .5f, 60, 20));
		}

		if (Main.rand.NextBool(12))
		{
			float mag = Main.rand.NextFloat();

			var rect = player.getRect();
			rect.Inflate(50, 50);

			var position = Main.rand.NextVector2FromRectangle(rect);
			var newCol = Color.Lerp(Color.Orange * .5f, Color.Gold, mag);
			float scale = MathHelper.Lerp(.1f, .3f, mag);

			ParticleHandler.SpawnParticle(new GlowParticle(position, Vector2.UnitY * mag * -.5f, newCol, scale, 80, 5));
			ParticleHandler.SpawnParticle(new GlowParticle(position, Vector2.UnitY * mag * -.5f, Color.White, scale * .5f, 80, 5));
		}
	}
}

internal class UndeadModPlayer : ModPlayer
{
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (Player.HasEquip<SafekeeperRing>() && UndeadNPC.IsUndeadType(target.type))
			modifiers.FinalDamage *= 1.25f;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		const int particleTime = 15;

		if (Main.dedServ)
			return;

		if (Player.HasEquip<SafekeeperRing>() && UndeadNPC.IsUndeadType(target.type) && target.life < MathHelper.Min(100, target.lifeMax * .25f))
		{
			var points = new Vector2[Main.rand.Next(2, 4)];

			for (int i = 0; i < points.Length; i++)
				points[i] = Main.rand.NextVector2FromRectangle(target.getRect());

			for (int i = 0; i < points.Length; i++)
				ParticleHandler.SpawnParticle(new HolyStar(points[i], Color.White, .5f, particleTime) { Scale = MathHelper.Lerp(1f, .5f, i / (points.Length - 1f)) });

			float rand = Main.rand.NextFloat();
			var pos = points[0];

			var circle = new TexturedPulseCircle(pos, (Color.Goldenrod * .5f).Additive(), 2, 42, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
			ParticleHandler.SpawnParticle(circle);

			var circle2 = new TexturedPulseCircle(pos, (Color.White * .5f).Additive(), 1, 40, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
			ParticleHandler.SpawnParticle(circle2);

			SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { Pitch = .5f }, target.Center);
		}
	}
}
