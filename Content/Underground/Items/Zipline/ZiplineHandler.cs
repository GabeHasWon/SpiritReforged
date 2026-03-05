using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.Items.Zipline;

internal class ZiplineHandler : ILoadable
{
	internal static Asset<Texture2D> HookTexture;
	internal static Asset<Texture2D> WireTexture;

	/// <summary> <see cref="Zipline"/>s belonging to all players.<para/>
	/// Use <see cref="Add"/> and <see cref="Zipline.RemovePoint"/> instead of directly adding and removing points from this set. </summary>
	[WorldBound]
	public static readonly HashSet<Zipline> Ziplines = [];

	/// <summary> Creates a new zipline at <paramref name="position"/> or adds to an existing zipline belonging to <paramref name="player"/>. </summary>
	/// <param name="player"> The zipline owner. </param>
	/// <param name="position"> The position to deploy at. </param>
	public static void Add(Player player, Vector2 position)
	{
		var line = Ziplines.Where(x => x.Owner == player).FirstOrDefault();

		if (line == default) //Add a new zipline
		{
			var newLine = new Zipline(player.whoAmI);
			newLine.points.Add(position);

			Ziplines.Add(newLine);
		}
		else
		{
			if (line.DrawingLine)
			{
				line.points[0] = line.points[1];
				line.points[1] = position;
			}
			else
				line.points.Add(position);
		}
	}

	public void Load(Mod mod)
	{
		On_Main.DoDraw_Tiles_NonSolid += DrawAllZiplines;

		HookTexture = DrawHelpers.RequestLocal(GetType(), "Zipline_Hook", false);
		WireTexture = DrawHelpers.RequestLocal(GetType(), "Zipline_Wire", false);
	}

	private static void DrawAllZiplines(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
	{
		orig(self);

		foreach (var zipline in Ziplines)
			zipline.Draw(Main.spriteBatch);
	}

	public static bool CheckZipline(Player player, out Zipline thisZipline)
	{
		thisZipline = null;
		foreach (var zipline in Ziplines)
		{
			if (zipline.OnZipline(player))
			{
				thisZipline = zipline;
				return true;
			}
		}

		return false;
	}

	public void Unload() { }
}

/// <summary> Exists to override <see cref="ModPlayer.PreUpdateMovement"/> and call <see cref="ZiplineHandler.CheckZipline"/>. </summary>
internal class ZiplinePlayer : ModPlayer
{
	public bool assistant = true;

	private bool _wasOnZipline;
	private bool _fast;

	public override void PreUpdateMovement()
	{
		bool onZipline = ZiplineHandler.CheckZipline(Player, out var line);

		if (onZipline)
		{
			float speedLimit = Math.Max(Player.maxRunSpeed * 2.2f, Player.accRunSpeed);

			if (Player.velocity.Length() < speedLimit)
				Player.velocity *= 1.05f + Math.Abs(line.Angle() / 20f);

			_wasOnZipline = true;
			Player.wet = false;

			if (Player.controlDown)
				Player.GetModPlayer<CollisionPlayer>().fallThrough = true;
		}

		if (!onZipline && _wasOnZipline)
			_wasOnZipline = false;
	}

	/// <summary> Zipline visuals and sounds associated with this player. </summary>
	public void DoEffects(Vector2 start, Vector2 end)
	{
		if (Math.Abs(Player.velocity.X) <= Player.maxRunSpeed + .1f)
			return;

		if (Main.timeForVisualEffects % 3 == 0)
			ParticleHandler.SpawnParticle(new LightningParticle(start, end, Color.Red, 30, 8f));

		if (Main.rand.NextBool(3))
			Dust.NewDustPerfect(start, DustID.Torch, Scale: 2).noGravity = true;

		if (Math.Abs(Player.velocity.X) > Player.accRunSpeed)
		{
			if (Main.timeForVisualEffects % 5 == 0)
			{
				float lerp = (Math.Abs(Player.velocity.X) - Player.accRunSpeed) / 2f;
				float pitch = MathHelper.Lerp(0f, .25f, lerp);

				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricSting") with { Volume = .3f, Pitch = pitch, PitchVariance = .2f }, start);
			}

			if (!_fast)
			{
				ParticleHandler.SpawnParticle(new TexturedPulseCircle(end, Color.White, Color.Red, .25f, 80f, 20, "supPerlin", Vector2.Zero, Common.Easing.EaseFunction.EaseCircularOut).WithSkew(.5f, end.AngleTo(start)));
				SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = .5f }, start);

				_fast = true;
			}

			if (Main.rand.NextBool())
				Dust.NewDustPerfect(start, DustID.MinecartSpark, (Player.velocity * -Main.rand.NextFloat() - Vector2.UnitY).RotatedByRandom(1), Scale: 2);
		}
		else
		{
			if (!_wasOnZipline && Math.Abs(Player.velocity.X) > Player.accRunSpeed - 2)
				SoundEngine.PlaySound(SoundID.Item52 with { Pitch = .6f }, start);

			_fast = false;
		}
	}

	public override void SaveData(TagCompound tag) => tag[nameof(assistant)] = assistant;
	public override void LoadData(TagCompound tag) => assistant = tag.GetBool(nameof(assistant));
}
