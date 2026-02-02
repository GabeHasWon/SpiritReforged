using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.PlayerCommon;

public class ModDash : ILoadable
{
	public readonly record struct DashInfo(int Duration, int Cooldown, float MaximumSpeed, EaseFunction Easing, float Decay);
	internal static readonly Dictionary<string, ModDash> DashByName = [];

	public string Name => GetType().Name;

	public void Load(Mod mod) => DashByName.Add(GetType().Name, this);
	public void Unload() { }

	public virtual void SetDefaults(out DashInfo info) => info = default;
	public virtual void DashEffects(Player player) { }
}

public sealed class DashPlayer : ModPlayer
{
	internal class ModDashData : PacketData
	{
		private readonly short _playerIndex;
		private readonly string _dashName;

		public ModDashData() { }
		public ModDashData(short playerIndex, string dashName)
		{
			_playerIndex = playerIndex;
			_dashName = dashName;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			short playerIndex = reader.ReadInt16();
			string dashName = reader.ReadString();

			if (Main.netMode == NetmodeID.Server)
				new ModDashData(playerIndex, dashName).Send(ignoreClient: whoAmI);

			if (Main.player[playerIndex].TryGetModPlayer(out DashPlayer dashPlayer))
				dashPlayer.EnableDash(_dashName);
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_playerIndex);
			modPacket.Write(_dashName);
		}
	}

	/// <summary> Gets whether this player can initiate a dash. </summary>
	public bool CanDash => !Player.shimmering && cooldown == 0;
	/// <summary> Gets the completion progress of the current dash within a range from 0 to 1. </summary>
	public float DashProgress => 1f - (float)(duration / (float)dashInfo.Duration);

	public ModDash ActiveDash { get; private set; }
	public Vector2 DashDirection { get; private set; }

	public ModDash.DashInfo dashInfo;
	public int duration;
	public int cooldown;

	public void EnableDash<T>(Vector2 direction) where T : ModDash
	{
		DashDirection = direction;
		EnableDash(typeof(T).Name);
	}

	public void EnableDash<T>(DoubleTapPlayer.Direction direction) where T : ModDash
	{
		DashDirection = DoubleTapPlayer.ConvertDirection(direction);
		EnableDash(typeof(T).Name);
	}

	public void EnableDash(string name)
	{
		if (CanDash)
		{
			ActiveDash = ModDash.DashByName[name];
			ActiveDash.SetDefaults(out dashInfo);

			duration = dashInfo.Duration;

			if (Main.netMode != NetmodeID.SinglePlayer)
				new ModDashData((short)Player.whoAmI, name).Send();
		}
	}

	public override void ResetEffects()
	{
		duration = Math.Max(duration - 1, 0);
		cooldown = Math.Max(cooldown - 1, 0);
	}

	public override void PostUpdateEquips()
	{
		if (ActiveDash == null)
			return;

		if (duration < 1 || Player.shimmering)
		{
			ActiveDash = null;
		}
		else
		{
			Player.vortexStealthActive = false;
			cooldown = dashInfo.Cooldown;

			if (Math.Abs(Player.velocity.X) > dashInfo.MaximumSpeed)
			{
				Player.velocity.X *= dashInfo.Decay;
			}
			else
			{
				float speed = dashInfo.MaximumSpeed * dashInfo.Easing.Ease(DashProgress);

				if (Player.velocity.Length() < speed || DashProgress > 0.5f)
					Player.velocity = DashDirection * speed;
			}

			if (DashDirection.Y < 0)
				Player.fallStart = (int)(Player.position.Y / 16); //Reset fall damage calculation if ascending

			ActiveDash.DashEffects(Player);
		}
	}
}