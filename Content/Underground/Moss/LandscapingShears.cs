using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon.CheckItemUse;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SpiritReforged.Content.Underground.Moss;

public class LandscapingShears : ModItem
{
	public const int AnimationMax = 15;

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.BestiaryGirl, new NPCShop.Entry(Type)));
	public override void SetDefaults()
	{
		Item.Size = new Vector2(24);
		Item.useTime = 5;
		Item.useAnimation = AnimationMax;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.buyPrice(0, 2, 50, 0);
		Item.rare = ItemRarityID.Green;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<LandscapingShearsHeld>();
		Item.shootSpeed = 1f;
		Item.DamageType = DamageClass.MeleeNoSpeed;
		Item.damage = 10;
		Item.knockBack = 1;
	}
}

internal class LandscapingShearsHeld : ModProjectile
{
	public float Progress => (float)Projectile.timeLeft / LandscapingShears.AnimationMax;

	public static readonly SoundStyle Clip = new("SpiritReforged/Assets/SFX/Item/Clippers")
	{
		PitchVariance = .4f
	};

	public override LocalizedText DisplayName => ModContent.GetInstance<LandscapingShears>().DisplayName;
	public override void SetStaticDefaults() => Main.projFrames[Type] = 2;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(36);
		Projectile.DamageType = DamageClass.MeleeNoSpeed;
		Projectile.friendly = true;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.scale = 0.8f;
		Projectile.timeLeft = LandscapingShears.AnimationMax;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		int holdDistance = 16 + (int)(Math.Sin(Progress * 3.14f) * 10f);
		float rotation = Projectile.velocity.ToRotation();
		var position = owner.MountedCenter + new Vector2(holdDistance, 0).RotatedBy(rotation);

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		Projectile.Center = owner.RotatedRelativePoint(position);
		Projectile.rotation = rotation;
		Projectile.frame = Math.Min((int)(Progress * 1.8f), 1);

		owner.heldProj = Projectile.whoAmI;
		owner.direction = (Projectile.velocity.X < 0) ? -1 : 1;
		owner.itemAnimation = owner.itemTime = 2;

		float armRotation = Projectile.rotation - 1.57f;

		if ((int)(Progress * 10f) == 4)
			SoundEngine.PlaySound(Clip, Projectile.Center);

		float spread = 1f;
		if (Projectile.frame == 0)
		{
			Projectile.scale = Math.Min(Projectile.scale + 0.04f, 1.05f);
			spread = 0.2f;
		}

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + spread * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation - spread * owner.direction);
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		Main.EntitySpriteDraw(texture, position, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects);
		return false;
	}
}

/// <summary> Handles vanilla tile item drops for <see cref="LandscapingShears"/>. </summary>
internal class LandscaperTile : GlobalTile
{
	private static readonly HashSet<int> HitTypes = [TileID.MushroomGrass, TileID.ArgonMoss, TileID.BlueMoss, TileID.BrownMoss, TileID.GreenMoss, 
		TileID.KryptonMoss, TileID.LavaMoss, TileID.PurpleMoss, TileID.RainbowMoss, TileID.RedMoss, TileID.VioletMoss, TileID.XenonMoss, 
		ModContent.TileType<OganessonMoss>(), ModContent.TileType<RadonMoss>()];

	private static readonly HashSet<int> CutTypes = [TileID.MushroomPlants, TileID.LongMoss, TileID.JunglePlants, TileID.JunglePlants2, TileID.Plants, TileID.Plants2];

	public override void Load() => On_Player.Update += ShowHoverIcon;
	private static void ShowHoverIcon(On_Player.orig_Update orig, Player self, int i)
	{
		orig(self, i);

		if (self.whoAmI == Main.myPlayer && self.HeldItem is Item held && held.type == ModContent.ItemType<LandscapingShears>())
		{
			var target = new Point(Player.tileTargetX, Player.tileTargetY);
			var tile = Main.tile[target.X, target.Y];

			if (tile.HasTile && self.InInteractionRange(target.X, target.Y, TileReachCheckSettings.Simple) && (CutTypes.Contains(tile.TileType) || HitTypes.Contains(tile.TileType)))
			{
				self.cursorItemIconEnabled = true;
				self.cursorItemIconID = ItemID.None;
				self.noThrow = 2;
			}
		}
	}

	#region add delegates
	public override void SetStaticDefaults()
	{
		foreach (int type in HitTypes)
			CheckItem.RegisterTileCheck(type, HitCheck);

		foreach (int type in CutTypes)
			CheckItem.RegisterTileCheck(type, CutCheck);
	}

	/// <summary> Drops the item associated with a tile and doesn't kill it. </summary>
	private static bool? HitCheck(int itemType, int i, int j)
	{
		if (itemType == ModContent.ItemType<LandscapingShears>())
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool())
			{
				var t = Main.tile[i, j];
				int oldType = t.TileType;

				WorldGen.KillTile(i, j, true);

				if (t.TileType != oldType) //Checks if the operation was successful
				{
					int drop = GetItems(i, j, oldType);
					Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), drop);
				}
			}
			else
			{
				WorldGen.KillTile(i, j, true);
			}

			return true;
		}

		return null;
	}

	/// <summary> Kills a tile. </summary>
	private static bool? CutCheck(int itemType, int i, int j)
	{
		if (itemType == ModContent.ItemType<LandscapingShears>())
		{
			WorldGen.KillTile(i, j);
			return true;
		}

		return null;
	}
	#endregion

	public override void Drop(int i, int j, int type)
	{
		if (!CutTypes.Contains(type))
			return;

		var p = Main.player[Player.FindClosest(new Vector2(i, j) * 16, 16, 16)];

		if (p.HeldItem.type == ModContent.ItemType<LandscapingShears>() && Main.rand.NextBool())
		{
			int drop = GetItems(i, j, type);
			Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(), drop);
		}
	}

	private static int GetItems(int i, int j, int tileType)
	{
		var t = Main.tile[i, j];
		int drop = TileLoader.GetItemDropFromTypeAndStyle(tileType);

		if (tileType is TileID.LongMoss)
			drop = (t.TileFrameX / 22) switch
			{
				0 => ItemID.GreenMoss,
				1 => ItemID.BrownMoss,
				2 => ItemID.RedMoss,
				3 => ItemID.BlueMoss,
				4 => ItemID.PurpleMoss,
				5 => ItemID.LavaMoss,
				6 => ItemID.KryptonMoss,
				7 => ItemID.XenonMoss,
				8 => ItemID.ArgonMoss,
				9 => ItemID.VioletMoss,
				_ => ItemID.RainbowMoss
			}; //Set drops manually because the prior method can't read them

		if (tileType is TileID.JunglePlants or TileID.JunglePlants2)
			drop = ItemID.JungleGrassSeeds;

		if (tileType is TileID.Plants or TileID.Plants2)
			drop = ItemID.Seed;

		if (tileType is TileID.MushroomPlants)
			drop = ItemID.GlowingMushroom;

		if (tileType is TileID.MushroomGrass)
			drop = ItemID.MushroomGrassSeeds;

		return drop;
	}
}