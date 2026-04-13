using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

internal class ScarabMountItem : ModItem
{
	public class ScarabMountBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.mount.SetMount(ModContent.MountType<ScarabMount>(), player, false);
			player.buffTime[buffIndex] = 10;
		}
	}

	public class ScarabMount : ModMount
	{
		private float _rotation = 0f;
		private int[] _hitsPerNPC = new int[Main.maxNPCs];

		public override void SetStaticDefaults()
		{
			MountData.buff = ModContent.BuffType<ScarabMountBuff>();
			MountData.spawnDust = DustID.Obsidian;
			MountData.spawnDustNoGravity = true;
			MountData.heightBoost = 34;
			MountData.fallDamage = 1f;
			MountData.runSpeed = 8;
			MountData.flightTimeMax = 0;
			MountData.fatigueMax = 0;
			MountData.jumpHeight = 0;
			MountData.acceleration = 0.16f;
			MountData.swimSpeed = 2;
			MountData.jumpSpeed = 8;
			MountData.blockExtraJumps = true;
			MountData.totalFrames = 1;
			MountData.constantJump = false;
			MountData.playerYOffsets = [40];
			MountData.yOffset = 15;
			MountData.xOffset = 0;
			MountData.bodyFrame = 3;
			MountData.playerHeadOffset = 26;
			MountData.standingFrameCount = 1;
			MountData.standingFrameDelay = 12;
			MountData.standingFrameStart = 0;
			MountData.inAirFrameCount = 1;
			MountData.inAirFrameDelay = 12;
			MountData.inAirFrameStart = 0;
			MountData.idleFrameCount = 1;
			MountData.idleFrameDelay = 12;
			MountData.idleFrameStart = 0;
			MountData.idleFrameLoop = true;

			if (Main.netMode != NetmodeID.Server)
			{
				MountData.textureWidth = MountData.backTexture.Width();
				MountData.textureHeight = MountData.backTexture.Height();
			}
		}

		public override void UpdateEffects(Player player)
		{
			SetStaticDefaults();

			player.noKnockback = true;
			_rotation += player.velocity.X * 0.02f;

			if (Main.getGoodWorld)
			{
				if (player.HasBuff(BuffID.Tipsy))
				{
					player.fullRotation = _rotation;
					player.fullRotationOrigin = new Vector2(10, 90);
				}
				else
					player.fullRotation = 0;
			}

			float xVel = Math.Abs(player.velocity.X);

			if (xVel > 6)
			{
				float chance = Utils.GetLerpValue(6, 8, xVel, true) * 0.4f;

				if (chance > Main.rand.NextFloat())
				{
					Point16 bottom = player.Bottom.ToTileCoordinates16();
					Tile tile = Main.tile[bottom];

					if (!tile.HasTile)
						return;

					int dust = WorldGen.KillTile_MakeTileDust(bottom.X, bottom.Y, tile);

					Main.dust[dust].position = player.Bottom;
					Main.dust[dust].velocity = new Vector2(-player.velocity.X * 0.35f, Main.rand.NextFloat(-4, -1));
				}
			}

			for (int i = 0; i < _hitsPerNPC.Length; ++i)
				_hitsPerNPC[i] = Math.Max(0, _hitsPerNPC[i] - 1);

			if (!ScarabMountPlayer.DashSpeed(player))
				return;

			Rectangle hitbox = player.Hitbox;
			hitbox.Inflate(12, 6);

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.isLikeATownNPC && !player.npcTypeNoAggro[npc.type] && (!npc.friendly || npc.lifeMax == 5) && npc.Hitbox.Intersects(hitbox) && _hitsPerNPC[npc.whoAmI] == 0)
				{
					_hitsPerNPC[npc.whoAmI] = 80;
					npc.SimpleStrikeNPC(30, Math.Sign(player.velocity.X), false, 12, damageVariation: true);
				}
			}
		}

		public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, 
			ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
		{
			rotation = _rotation;

			return true;
		}
	}

	public class ScarabMountPlayer : ModPlayer
	{
		public static bool MountDashing(Player plr) => plr.mount.Active && plr.mount.Type == ModContent.MountType<ScarabMount>() && DashSpeed(plr);
		public static bool DashSpeed(Player plr) => Math.Abs(plr.velocity.X) >= plr.mount._data.runSpeed - 1f;

		public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
		{
			Player plr = drawInfo.drawPlayer;

			if (Player.mount.Active && plr.mount.Type == ModContent.MountType<ScarabMount>())
			{
				drawInfo.isSitting = true;

				if (DashSpeed(Player))
				{
					Player.armorEffectDrawShadowLokis = true;
					Player.armorEffectDrawOutlines = true;
				}
			}
		}

		public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
		{
			if (MountDashing(Player))
				return false;

			return true;
		}
	}

	public class ScarabSaddleLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player plr = drawInfo.drawPlayer;

			if (!plr.mount.Active || plr.mount.Type != ModContent.MountType<ScarabMount>() || drawInfo.shadow != 0)
				return;

			Vector2 position = drawInfo.Center - Main.screenPosition;
			drawInfo.DrawDataCache.Add(new DrawData(Saddle.Value, position.Floor() - new Vector2(16, -18), null, Color.White, 0f, Vector2.Zero, 1f, drawInfo.playerEffect, 0)
			{
				shader = plr.cMount
			});
		}
	}

	private static readonly Asset<Texture2D> Saddle = DrawHelpers.RequestLocal(typeof(ScarabMount), "ScarabMount_Saddle", false);

	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 26;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Yellow;
		Item.maxStack = 1;
		Item.UseSound = SoundID.Item79;
		Item.mountType = ModContent.MountType<ScarabMount>();
		Item.useTime = Item.useAnimation = 20;
	}
}
