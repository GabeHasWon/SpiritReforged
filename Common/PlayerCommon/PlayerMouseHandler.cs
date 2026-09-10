using SpiritReforged.Common.Multiplayer;

namespace SpiritReforged.Common.PlayerCommon;

public static class PlayerMouseHandler
{
	private static readonly Dictionary<int, Vector2> _MouseByWhoAmI = [];

	/// <summary> Gets <see cref="Main.MouseWorld"/> from the client <paramref name="who"/>. <br/>
	/// <paramref name="refresh"/> automatically updates the cached position for client <paramref name="who"/>. <para/>
	/// <b>DO NOT</b> use this for syncing-important content, only for unimportant visuals or vfx. </summary>
	public static Vector2 GetMouse(int who, bool refresh = true)
	{
		if (Main.myPlayer == who)
		{
			return Main.MouseWorld;
		}
		else if (refresh)
		{
			MultiplayerLoader.Send(nameof(RequestMousePosition), -1, -1, Main.myPlayer, who);
		}

		if (_MouseByWhoAmI.TryGetValue(who, out Vector2 mouse))
		{
			return mouse;
		}

		return Main.player[who].Center;
	}

	/// <summary> Requests the mouse position of <paramref name="requestedPlayer"/> and sends it back to <paramref name="requestingPlayer"/>.<br/>
	/// Expected to be sent by multiplayer clients only. </summary>
	[NetSynced(Log: false)]
	public static void RequestMousePosition(int requestingPlayer, int requestedPlayer)
	{
		if (Main.dedServ)
			MultiplayerLoader.Send(nameof(RequestMousePosition), requestedPlayer, -1, requestingPlayer, requestedPlayer); //(1) Recieved by the server after GetMouse was called, send to fromPlayer for Main.MouseWorld
		else if (Main.myPlayer == requestedPlayer)
			MultiplayerLoader.Send(nameof(SendMousePosition), -1, -1, requestingPlayer, requestedPlayer, Main.MouseWorld); //(2) Recieved by toPlayer, send to server
	}

	[NetSynced(Log: false)]
	public static void SendMousePosition(int requestingPlayer, int requestedPlayer, Vector2 position)
	{
		if (Main.dedServ)
			MultiplayerLoader.Send(nameof(SendMousePosition), requestingPlayer, -1, requestingPlayer, requestedPlayer, position); //(3) Recieved by the server, send to fromPlayer

		if (!_MouseByWhoAmI.TryAdd(requestedPlayer, position))
			_MouseByWhoAmI[requestedPlayer] = position;
	}
}