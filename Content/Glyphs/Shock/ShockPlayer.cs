using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;

namespace SpiritReforged.Content.Glyphs.Shock;

public partial class ShockGlyph
{
	public sealed class ShockPlayer : ModPlayer
	{
		private class ShockPacket : PacketData
		{
			private readonly short _player;
			private readonly short _npc;
			private readonly int _damage;

			public ShockPacket() : base() { }

			public ShockPacket(short npc, short player, int damage)
			{
				_npc = npc;
				_player = player;
				_damage = damage;
			}

			public override void OnReceive(BinaryReader reader, int whoAmI)
			{
				short npc = reader.ReadInt16();
				short player = reader.ReadInt16();
				int damage = reader.ReadInt32();

				ChannelLightning(Main.player[player], Main.npc[npc], damage);
			}

			public override void OnSend(ModPacket modPacket)
			{
				modPacket.Write(_npc);
				modPacket.Write(_player);
				modPacket.Write(_damage);
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && Main.myPlayer == Player.whoAmI) 
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					new ShockPacket((short)target.whoAmI, (short)Player.whoAmI, damageDone).Send(ignoreClient: Player.whoAmI);

				ChannelLightning(Player, target, damageDone);
			}			
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && proj.type != ModContent.ProjectileType<ShockGlyphLightningBolt>() && Main.myPlayer == Player.whoAmI)
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					new ShockPacket((short)target.whoAmI, (short)Player.whoAmI, damageDone).Send(ignoreClient: Player.whoAmI);

				ChannelLightning(Player, target, damageDone);
			}
		}

		public static void ChannelLightning(Player Player, NPC target, int damage)
		{
			SoundEngine.PlaySound(ElectricSting, target.Center);
			SoundEngine.PlaySound(ElectricZap, target.Center);

			ScreenshakeHelper.Shake(target.Center, Main.rand.NextVector2Circular(1f, 1f), 1, 4, 10);

			for (int i = 0; i < 3; i++)
			{
				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 60)));

				Vector2 pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Yellow.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));

				pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Cyan.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));
			}

			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(target.Center, ModContent.DustType<YellowElectricDust>(), Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;

				Dust.NewDustPerfect(target.Center, DustID.Electric, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;
			}

			static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;

			NPC[] closestNPCs = Main.npc.Where(n => n.whoAmI != target.whoAmI && n.CanBeChasedBy(Player) && n.DistanceSQ(target.Center) < 350000f).OrderBy(n => n.DistanceSQ(target.Center)).Take(3).ToArray();

			if (closestNPCs.Length <= 0)
				return;

			for (int i = 0; i < closestNPCs.Length; i++)
			{
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero,
					ModContent.ProjectileType<ShockGlyphLightningBolt>(), (Main.hardMode ? 1 : 5) + (int)(damage * (Main.hardMode ? 0.25f : 0.35f)), 1f, Player.whoAmI, closestNPCs[i].whoAmI, ai2: i == 0 ? 1 : 0);
			}
		}

		//25% damage increase with critical hits for single target utility (not sure if needs to be synced??)
		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				modifiers.CritDamage += 0.5f;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				modifiers.CritDamage += 0.5f;
		}
	}
}