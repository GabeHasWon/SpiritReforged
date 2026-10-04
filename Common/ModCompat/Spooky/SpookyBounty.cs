using SpiritReforged.Common.BuffCommon.Stacking;
using SpiritReforged.Common.Multiplayer;
using System.Collections;
using System.IO;
using System.Xml.Linq;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal abstract class SpookyBounty : ILoadable, ILocalizedModType
{
	public class SpookyBountySaving : ModSystem
	{
		public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod("Spooky");

		public override void LoadWorldData(TagCompound tag)
		{
			tag.Add("dhampirActive", DhampirBounty.BountyActive);
			tag.Add("mummyActive", MummyBounty.BountyActive);
		}

		public override void SaveWorldData(TagCompound tag)
		{
			DhampirBounty.BountyActive = tag.GetBool("dhampirActive");
			MummyBounty.BountyActive = tag.GetBool("mummyActive");
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(DhampirBounty.BountyActive);
			writer.Write(MummyBounty.BountyActive);
		}

		public override void NetReceive(BinaryReader reader)
		{
			DhampirBounty.BountyActive = reader.ReadBoolean();
			MummyBounty.BountyActive = reader.ReadBoolean();
		}
	}

	public class SpookyBountyData : PacketData
	{
		private readonly int _index = 0;

		public SpookyBountyData() { }
		public SpookyBountyData(int index) => _index = index;

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			byte index = 0;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);

			if (index == 0)
				DhampirBounty.BountyActive = true;
			else
				MummyBounty.BountyActive = true;
		}

		public override void OnSend(ModPacket modPacket) => modPacket.Write((byte)_index);
	}

	public enum DialogueType : byte
	{
		Encounter,
		Recover,
		Complete
	}

	string ILocalizedModType.LocalizationCategory => $"CrossMod.Spooky";

	public Mod Mod => SpiritReforgedMod.Instance;
	public string Name => GetType().Name;
	public string FullName => Mod.Name + "/" + Name;

	protected abstract int DialogueLength { get; }
	protected abstract int RecoverDialogueLength { get; }
	protected abstract int CompleteLength { get; }

	protected LocalizedText[] LoadedPlayerText;
	protected LocalizedText[] LoadedEyeText;

	protected LocalizedText[] LoadedPlayerRecover;
	protected LocalizedText[] LoadedEyeRecover;

	protected LocalizedText[] LoadedPlayerComplete;
	protected LocalizedText[] LoadedEyeComplete;

	public static int GetLittleEyeIndex()
	{
		if (!CrossMod.Spooky.Enabled)
			return -1;

		return NPC.FindFirstNPC(CrossMod.Spooky.Find<ModNPC>("LittleEye").Type);
	}

	bool ILoadable.IsLoadingEnabled(Mod mod) => ModLoader.HasMod("Spooky");

	public void Load(Mod mod)
	{
		LoadedPlayerText = new LocalizedText[DialogueLength];
		LoadedEyeText = new LocalizedText[DialogueLength];
		LoadedEyeRecover = new LocalizedText[RecoverDialogueLength];
		LoadedPlayerRecover = new LocalizedText[RecoverDialogueLength];
		LoadedPlayerComplete = new LocalizedText[CompleteLength];
		LoadedEyeComplete = new LocalizedText[CompleteLength];

		for (int i = 0; i < DialogueLength; i++)
		{
			LoadedEyeText[i] = this.GetLocalization("Dialogue.Eye." + i);
			LoadedPlayerText[i] = this.GetLocalization("Dialogue.Player." + i);
		}

		for (int i = 0; i < RecoverDialogueLength; i++)
		{
			LoadedEyeRecover[i] = this.GetLocalization("RecoveryDialogue.Eye." + i);
			LoadedPlayerRecover[i] = this.GetLocalization("RecoveryDialogue.Player." + i);
		}

		for (int i = 0; i < CompleteLength; i++)
		{
			LoadedEyeComplete[i] = this.GetLocalization("CompletionDialogue.Eye." + i);
			LoadedPlayerComplete[i] = this.GetLocalization("CompletionDialogue.Player." + i);
		}

		InnerLoad();
	}

	/// <summary>
	/// Maps the loaded dialogue to a (string, string)[] based on the given <paramref name="type"/>.
	/// </summary>
	public (string, string)[] MapDialogue(DialogueType type)
	{
		if (type == DialogueType.Encounter)
		{
			var pairs = new (string, string)[RecoverDialogueLength];

			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeRecover[i].Value, LoadedPlayerRecover[i].Value);

			return pairs;
		}
		else if (type == DialogueType.Recover)
		{
			var pairs = new (string, string)[DialogueLength];
			
			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeText[i].Value, LoadedPlayerText[i].Value);

			return pairs;
		}
		else
		{
			var pairs = new (string, string)[CompleteLength];
			
			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeComplete[i].Value, LoadedPlayerComplete[i].Value);

			return pairs;
		}
	}

	protected virtual void InnerLoad()
	{
	}

	public virtual void Unload()
	{
	}
}
