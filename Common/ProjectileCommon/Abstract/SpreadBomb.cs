using SpiritReforged.Common.WorldGeneration;
using Terraria.Audio;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.ProjectileCommon.Abstract;

/// <summary> Used for building block spread bombs like <see cref="ProjectileID.DirtBomb"/>. </summary>
public abstract class SpreadBomb : BombProjectile
{
	public int dustType;
	public int tileType;

	public override void FuseVisuals()
	{
		if (Main.rand.NextBool())
		{
			var position = Projectile.Center - (new Vector2(0, Projectile.height / 2 + 10) * Projectile.scale).RotatedBy(Projectile.rotation);

			var dust = Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Unit(), 100);
			dust.scale = 0.1f + Main.rand.NextFloat(0.5f);
			dust.fadeIn = 1.5f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;

			dust = Dust.NewDustPerfect(position, DustID.Torch, Main.rand.NextVector2Unit(), 100);
			dust.scale = 1f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;

			dust = Dust.NewDustPerfect(position, dustType, Main.rand.NextVector2Unit(), 100);
			dust.scale = 1f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;
		}
	}

	public override void OnKill(int timeLeft)
	{
		Projectile.Resize(22, 22);

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			var color = Color.Transparent;

			for (int i = 0; i < 30; i++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 100, color, 1.5f);
				dust.velocity *= 1.4f;
			}

			for (int i = 0; i < 80; i++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0, 100, color, 2.2f);
				dust.noGravity = true;
				dust.velocity.Y -= 1.2f;
				dust.velocity *= 4f;

				var dust2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0, 100, color, 1.3f);
				dust2.velocity.Y -= 1.2f;
				dust2.velocity *= 2f;
			}

			for (int i = 1; i <= 2; i++)
			{
				for (int num852 = -1; num852 <= 1; num852 += 2)
				{
					for (int num853 = -1; num853 <= 1; num853 += 2)
					{
						var gore = Gore.NewGoreDirect(Projectile.GetSource_Death(), Projectile.position, Vector2.Zero, Main.rand.Next(61, 64));
						gore.velocity *= (i == 1) ? 0.4f : 0.8f;
						gore.velocity += new Vector2(num852, num853);
					}
				}
			}
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var pt = Projectile.Center.ToTileCoordinates();
			var data = new ShapeData();
			var circle = new Shapes.Circle(area);

			WorldUtils.Gen(pt, circle, new Actions.Custom(static (i, j, args) => WorldGen.TryKillingReplaceableTile(i, j, 0)));
			WorldUtils.Gen(pt, circle, Actions.Chain(new Modifiers.IsEmpty(), new Actions.SetTileKeepWall((ushort)tileType)).Output(data));

			WorldUtils.Gen(pt, new ModShapes.All(data), Actions.Chain(new Actions.SetFrames(frameNeighbors: true), new GenTypes.Send()));
		}
	}
}