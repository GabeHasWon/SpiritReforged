using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.PlayerCommon;

internal class PlayerMouseHandler
{
	internal class ShareMouseData : PacketData
	{
		public override bool Log => false;

		private readonly byte _playerWho;
		private readonly Vector2 _mouse;

		public ShareMouseData() { }

		public ShareMouseData(byte playerWho, Vector2 mouse)
		{
			_playerWho = playerWho;
			_mouse = mouse;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			byte who = reader.ReadByte();
			Vector2 mouse = reader.ReadVector2();

			if (Main.netMode == NetmodeID.Server)
				new ShareMouseData(who, mouse).Send(); //Relay to other clients

			if (!MouseByWhoAmI.TryAdd(who, mouse))
				MouseByWhoAmI[who] = mouse;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_playerWho);
			modPacket.WriteVector2(_mouse);
		}
	}

	public static Dictionary<int, Vector2> MouseByWhoAmI = [];
}
