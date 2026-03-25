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

	/// <summary>
	/// Gets either <see cref="Main.MouseWorld"/> for the local client, or <see cref="MouseByWhoAmI"/>[<paramref name="who"/>] for remote clients.<br/>
	/// Must be used in tandem with a <see cref="ShareMouseData"/> packet, otherwise the mouse data will not be updated properly.<br/>
	/// If clients have not yet recieved the mouse packet, this will default to a few tiles above the remove player.<br/>
	/// <b>DO NOT</b> use this for syncing-important content, only for unimportant visuals or vfx.
	/// </summary>
	public static Vector2 GetMouse(int who) => Main.myPlayer == who ? Main.MouseWorld : 
		(MouseByWhoAmI.TryGetValue(who, out Vector2 mouse) ? mouse : Main.player[who].Center - new Vector2(0, 40));
}
