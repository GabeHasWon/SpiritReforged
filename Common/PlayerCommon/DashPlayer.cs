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

	public float DashProgress => 1f - (float)(duration / (float)dashInfo.Duration);
	public ModDash ActiveDash { get; private set; }

	public ModDash.DashInfo dashInfo;
	public int duration;
	public int cooldown;

	public void EnableDash<T>() where T : ModDash => EnableDash(typeof(T).Name);
	public void EnableDash(string name)
	{
		if (cooldown == 0)
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

		if (duration < 1)
		{
			ActiveDash = null;
		}
		else
		{
			Player.vortexStealthActive = false;
			cooldown = dashInfo.Cooldown;

			if (Math.Abs(Player.velocity.X) > dashInfo.MaximumSpeed)
			{
				Player.velocity.X = Player.velocity.X * dashInfo.Decay;
			}
			else
			{
				float speed = dashInfo.MaximumSpeed * dashInfo.Easing.Ease(DashProgress);
				if (Math.Abs(Player.velocity.X) < speed || DashProgress > 0.5f)
					Player.velocity.X = speed * Player.direction;
			}

			ActiveDash.DashEffects(Player);
		}
	}
}