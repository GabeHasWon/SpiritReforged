namespace SpiritReforged.Common.ModCompat.Spooky;

internal abstract class SpookyBounty : ILoadable, ILocalizedModType
{
	string ILocalizedModType.LocalizationCategory => $"CrossMod.Spooky";

	public Mod Mod => SpiritReforgedMod.Instance;
	public string Name => GetType().Name;
	public string FullName => Mod.Name + "/" + Name;

	protected abstract int DialogueLength { get; }
	protected abstract int RecoverDialogueLength { get; }

	protected LocalizedText[] LoadedPlayerText;
	protected LocalizedText[] LoadedEyeText;

	protected LocalizedText[] LoadedPlayerRecover;
	protected LocalizedText[] LoadedNPCRecover;

	public static int GetLittleEyeIndex()
	{
		if (!CrossMod.Spooky.Enabled)
			return -1;

		return NPC.FindFirstNPC(CrossMod.Spooky.Find<ModNPC>("LittleEye").Type);
	}

	public void Load(Mod mod)
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		LoadedPlayerText = new LocalizedText[DialogueLength];
		LoadedEyeText = new LocalizedText[DialogueLength];
		LoadedNPCRecover = new LocalizedText[RecoverDialogueLength];
		LoadedPlayerRecover = new LocalizedText[RecoverDialogueLength];

		for (int i = 0; i < DialogueLength; i++)
		{
			LoadedEyeText[i] = this.GetLocalization("Dialogue.Eye." + i);
			LoadedPlayerText[i] = this.GetLocalization("Dialogue.Player." + i);
		}

		for (int i = 0; i < RecoverDialogueLength; i++)
		{
			LoadedNPCRecover[i] = this.GetLocalization("RecoveryDialogue.NPC." + i);
			LoadedPlayerRecover[i] = this.GetLocalization("RecoveryDialogue.Player." + i);
		}

		InnerLoad();
	}

	public (string, string)[] MapDialogue(bool recover)
	{
		if (recover)
		{
			var pairs = new (string, string)[RecoverDialogueLength];

			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedNPCRecover[i].Value, LoadedPlayerRecover[i].Value);

			return pairs;
		}
		else
		{
			var pairs = new (string, string)[DialogueLength];
			
			for (int i = 0; i < pairs.Length; i++)
				pairs[i] = (LoadedEyeText[i].Value, LoadedPlayerText[i].Value);

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
