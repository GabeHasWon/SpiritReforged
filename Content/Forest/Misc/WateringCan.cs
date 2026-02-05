using SpiritReforged.Common.ItemCommon;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Misc;

public class WateringCan : ModItem
{
	public class WateringCanHeld : ModProjectile
	{
		public int CurrentFrame
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override LocalizedText DisplayName => ModContent.GetInstance<WateringCan>().DisplayName;
		public override void SetStaticDefaults() => Main.projFrames[Type] = NumStyles;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(16);
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			float rotation = Projectile.velocity.ToRotation();
			float armRotation = rotation - 1.57f;
			Vector2 position = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);

			Projectile.direction = Projectile.spriteDirection = owner.direction;
			Projectile.Center = owner.RotatedRelativePoint(position);
			Projectile.rotation = rotation + 0.3f * Projectile.direction;

			owner.heldProj = Projectile.whoAmI;
			owner.itemAnimation = owner.itemTime = 2;
			owner.ChangeDir((Projectile.velocity.X < 0) ? -1 : 1);

			if (owner.channel)
				Projectile.timeLeft++;

			if (CheckArcCollision(Projectile.velocity - Vector2.UnitY * 0.5f, 0.2f, out Vector2 result))
				OnWaterLocation(result);

			if (!Main.dedServ)
			{
				Vector2 velocity = Projectile.velocity;
				Dust.NewDustPerfect(Projectile.Center + velocity * 20, DustID.Water, (velocity * 3).RotatedByRandom(0.2f));
			}

			if (Main.myPlayer == Projectile.owner)
			{
				Vector2 oldVelocity = Projectile.velocity;
				Projectile.velocity = owner.DirectionTo(Main.MouseWorld);

				if (oldVelocity != Projectile.velocity)
					Projectile.netUpdate = true;
			}

			owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		}

		public virtual void OnWaterLocation(Vector2 worldPosition) => Dust.NewDustPerfect(worldPosition, DustID.Water);

		/// <summary> Checks for tile collision starting from the projectile center, moving along <paramref name="force"/> affected by <paramref name="gravity"/> over time. </summary>
		public bool CheckArcCollision(Vector2 force, float gravity, out Vector2 result)
		{
			Rectangle hitbox = Projectile.Hitbox;
			result = Projectile.Center;

			for (int c = 0; c < 15; c++)
			{
				hitbox.Location += (force * 8).ToPoint();
				if (Collision.SolidCollision(hitbox.TopLeft(), hitbox.Width, hitbox.Height))
				{
					result = hitbox.Center();
					return true;
				}

				force += Vector2.UnitY * gravity;
			}

			return false;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Projectile.frame = CurrentFrame;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 position = new((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
			SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
			Vector2 origin = new(0, source.Height / 2);

			Main.EntitySpriteDraw(texture, position, source, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects);
			return false;
		}
	}

	protected override bool CloneNewInstances => true;

	public const int NumStyles = 8;
	public byte style = NumStyles;

	public override void SetStaticDefaults() => VariantGlobalItem.AddVariants(Type, NumStyles, false);

	public override void SetDefaults()
	{
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(silver: 20);
		Item.channel = true;
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<WateringCanHeld>();

		style = (byte)Main.rand.Next(NumStyles);
		SetVisualStyle();
	}

	public override ModItem Clone(Item itemClone)
	{
		var myClone = (WateringCan)base.Clone(itemClone);
		myClone.style = style;
		return myClone;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai0: style);
		return false;
	}

	public override void SaveData(TagCompound tag) => tag[nameof(style)] = style;
	public override void LoadData(TagCompound tag)
	{
		style = tag.Get<byte>(nameof(style));
		SetVisualStyle();
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(style);
	public override void NetReceive(BinaryReader reader)
	{
		style = reader.ReadByte();
		SetVisualStyle();
	}

	private void SetVisualStyle()
	{
		if (!Main.dedServ && Item.TryGetGlobalItem(out VariantGlobalItem v))
			v.subID = style;
	}
}