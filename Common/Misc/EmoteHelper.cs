using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.GameContent.UI;

namespace SpiritReforged.Common.Misc;

internal static class EmoteHelper
{
	internal class SendNPCEmote : PacketData
	{
		private readonly byte _who;
		private readonly short _time;
		private readonly short _type;
		private readonly byte _otherWhoType;
		private readonly short _otherWho;

		public SendNPCEmote() { }

		/// <summary>
		/// Creates a packet that sends <see cref="Emote(NPC, int, int, WorldUIAnchor)"/> with Main.npc[who], time, type, and other based on the given parameters.
		/// </summary>
		/// <param name="who">The whoAmI of the NPC to emote.</param>
		/// <param name="time">The amount of time for the NPC to emote.</param>
		/// <param name="type">The type of emote to use.</param>
		/// <param name="otherWhoType"><see cref="byte.MaxValue"/> for no other; 0 for player, 1 for npc.</param>
		/// <param name="otherWho">The whoAmI of the other entity. Unused if <see cref="_otherWhoType"/> is <see cref="byte.MaxValue"/>.</param>
		public SendNPCEmote(byte who, short time, short type, byte otherWhoType = byte.MaxValue, short otherWho = 0)
		{
			_who = who;
			_time = time;
			_type = type;
			_otherWho = otherWho;
			_otherWhoType = otherWhoType;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			byte who = reader.ReadByte();
			short time = reader.ReadInt16();
			short type = reader.ReadInt16();
			byte otherWhoType = reader.ReadByte();
			byte otherWho = 0;

			if (otherWho != byte.MaxValue)
			{
				reader.ReadInt16();
			}

			Emote(Main.npc[who], time, type, otherWhoType == byte.MaxValue ? null : GetWorldAnchor(otherWhoType, otherWho));
		}

		private static WorldUIAnchor GetWorldAnchor(byte otherWhoType, byte otherWho)
		{
			if (otherWhoType == 0)
				return new(Main.player[otherWho]);

			if (otherWhoType == 1)
				return new(Main.npc[otherWho]);

			return null;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_who);
			modPacket.Write(_time);
			modPacket.Write(_type);
			modPacket.Write(_otherWhoType);

			if (_otherWhoType != byte.MaxValue)
				modPacket.Write(_otherWho);
		}
	}

	/// <summary>
	/// Makes an emote bubble for the given NPC that lasts <paramref name="emoteTime"/> of emote type <paramref name="emoteType"/>.<br/>
	/// The NPC is used as the primary anchor.<br/>
	/// This should not be called on multiplayer clients.
	/// </summary>
	/// <param name="npc">NPC to emote.</param>
	/// <param name="emoteTime">How long, in ticks, the emote lasts.</param>
	/// <param name="emoteType">The type of emote to use. See <see cref="EmoteID"/>.</param>
	/// <param name="other">The other world anchor to use. <see cref="EmoteBubble.NewBubbleNPC(WorldUIAnchor, int, WorldUIAnchor)"/> describes this as:  
	/// The <see cref="WorldUIAnchor"/> instance from the other side of the conversation. <b>This can only be an NPC anchor,</b> despite allowing any entity.</param>
	public static void Emote(this NPC npc, int emoteTime, int emoteType, WorldUIAnchor other = null)
	{
		int oldNetMode = Main.netMode; // Vanilla forces it to sync if we're on the server, but we immediately need to sync it again.
		Main.netMode = NetmodeID.SinglePlayer; // This skips the needless sync.

		var bubbleAnchor = new WorldUIAnchor(npc);
		int bubbleWho = EmoteBubble.NewBubbleNPC(bubbleAnchor, emoteTime, other);
		var bubble = EmoteBubble.GetExistingEmoteBubble(bubbleWho);
		bubble.emote = emoteType; // Override the emote type since NewBubbleNPC is only random for some reason.

		Main.netMode = oldNetMode;

		if (Main.netMode == NetmodeID.Server) // Properly sync the new bubble.
		{
			Tuple<int, int> tuple = EmoteBubble.SerializeNetAnchor(bubbleAnchor);
			NetMessage.SendData(MessageID.SyncEmoteBubble, -1, -1, null, bubbleWho, tuple.Item1, tuple.Item2, emoteTime, bubble.emote, bubble.metadata);
		}
	}

	/// <summary>
	/// Calls <see cref="Emote(NPC, int, int, WorldUIAnchor)"/> on singleplayer/the server, or sends a <see cref="SendNPCEmote"/> packet if on a multiplayer client.
	/// </summary>
	/// <param name="npc">The NPC that is emoting.</param>
	/// <param name="time">The amount of time that the NPC emotes.</param>
	/// <param name="type">The type of emote to use.</param>
	/// <param name="otherEntityAnchor">The other entity anchor to use, if any. 
	/// This originally supported <see cref="Player"/> values, but vanilla doesn't account for it despite taking an Entity.</param>
	public static void SyncedEmote(NPC npc, int time, int type, NPC otherEntityAnchor = null)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			Emote(npc, time, type, otherEntityAnchor is null ? null : new WorldUIAnchor(otherEntityAnchor));
		else
		{
			byte who = (byte)npc.whoAmI;

			byte otherType = otherEntityAnchor switch
			{
				NPC => 1,
				_ => byte.MaxValue,
			};

			byte otherWho = (byte)(otherType == byte.MaxValue ? 0 : otherEntityAnchor.whoAmI);
			new SendNPCEmote(who, (short)time, (short)type, otherType, otherWho).Send();
		}
	}
}
