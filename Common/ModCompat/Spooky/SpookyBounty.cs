using SpiritReforged.Common.Multiplayer;
using System.Diagnostics;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal abstract class SpookyBounty : ILoadable, ILocalizedModType
{
	public class SpookyBountySaving : ModSystem
	{
		public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod("Spooky");

		public override void SaveWorldData(TagCompound tag) => tag.Add("bountyInfo", PackInfo());
		public override void LoadWorldData(TagCompound tag) => UnpackInfo(tag.GetByte("bountyInfo"));

		public override void NetSend(BinaryWriter writer) => writer.Write(PackInfo());
		public override void NetReceive(BinaryReader reader) => UnpackInfo(reader.ReadByte());

		internal static byte PackInfo()
		{
			byte packed = (byte)DhampirBounty.Active.ToInt();
			packed |= (byte)(DhampirBounty.Complete.ToInt() << 1);
			packed |= (byte)(MummyBounty.Active.ToInt() << 2);
			packed |= (byte)(MummyBounty.Complete.ToInt() << 3);
			return packed;
		}

		internal static void UnpackInfo(byte packed)
		{
			DhampirBounty.Active = (packed & 0b_1) == 0b_1;
			DhampirBounty.Complete = (packed & 0b_10) == 0b_10;
			MummyBounty.Active = (packed & 0b_100) == 0b_100;
			MummyBounty.Complete = (packed & 0b_1000) == 0b_1000;
		}
	}

	public class SpookyBountyData : PacketData
	{
		private readonly byte _data = 0;

		public SpookyBountyData() => _data = SpookyBountySaving.PackInfo();

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			byte packed = reader.ReadByte();
			SpookyBountySaving.UnpackInfo(packed);

			if (Main.netMode == NetmodeID.Server)
				new SpookyBountyData().Send();
		}

		public override void OnSend(ModPacket modPacket) => modPacket.Write(_data);
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
			var pairs = new (string, string)[DialogueLength];

			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeText[i].Value, LoadedPlayerText[i].Value);

			return pairs;
		}
		else if (type == DialogueType.Recover)
		{
			var pairs = new (string, string)[RecoverDialogueLength];
			
			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeRecover[i].Value, LoadedEyeRecover[i].Value);

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

	public static void QuickItem(NPC entity, int type, int stack = 1)
	{
		int number = Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, type, stack);

		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.SyncItem, -1, -1, null, number, 1f);
	}

	public static void CommonDrops(NPC entity)
	{
		if (Main.rand.NextBool(5))
		{
			QuickItem(entity, Main.rand.Next(3) switch
			{
				0 => ItemID.ChainKnife,
				1 => ItemID.BlackLens,
				2 => ItemID.Katana,
				_ => throw new UnreachableException()
			});
		}

		if (Main.rand.NextBool(2))
			QuickItem(entity, CrossMod.Spooky.Find<ModItem>("SnotMedication").Type);

		if (Main.rand.NextBool(3))
			QuickItem(entity, CrossMod.Spooky.Find<ModItem>("ThreeDimensionalGlasses").Type);

		if (Main.rand.NextBool(3))
			QuickItem(entity, ItemID.BloodMoonStarter);

		if (Main.rand.NextBool(3))
			QuickItem(entity, ItemID.ObsidianLockbox);

		if (Main.rand.NextBool(2))
		{
			QuickItem(entity, CrossMod.Spooky.Find<ModItem>(Main.rand.Next(3) switch
			{
				0 => "BlackLicorice",
				1 => "GoofyPretzel",
				2 => "EyeChocolate",
				_ => throw new UnreachableException(),
			}).Type);
		}

		QuickItem(entity, ItemID.GoldCoin, 10);
	}
}
