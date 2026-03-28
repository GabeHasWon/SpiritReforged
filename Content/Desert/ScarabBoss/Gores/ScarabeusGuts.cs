using SpiritReforged.Common.MathHelpers;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Gores;

public class ScarabeusGuts : ModGore
{
	public override void OnSpawn(Gore gore, IEntitySource source)
	{
		gore.numFrames = 6;
		gore.Frame = new(2, 3, 0, (byte)Main.rand.Next(3));
		gore.timeLeft = Gore.goreTime * 3;
	}

	public override bool Update(Gore gore)
	{
		gore.velocity.X *= 0.98f;
		gore.velocity.Y = Math.Min(gore.velocity.Y + 0.2f, 8);
		gore.rotation += gore.velocity.X * 0.1f;

		if (CollisionChecks.Tiles(new((int)gore.position.X, (int)gore.position.Y, 16, 16), CollisionChecks.SolidOrPlatform))
		{
			if (gore.drawOffset.Y == 0) //One-time effects
				SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.2f, Volume = 0.5f }, gore.position);

			gore.position.Y = gore.position.ToTileCoordinates().ToWorldCoordinates().Y;

			gore.drawOffset.Y = 0;
			gore.Frame = new(2, 3, 1, gore.Frame.CurrentRow);
			gore.velocity = Vector2.Zero;
			gore.rotation = 0;
		}

		if (--gore.timeLeft <= 0 && (gore.alpha += 5) >= 255)
			gore.active = false;

		gore.position += gore.velocity;
		return false;
	}
}