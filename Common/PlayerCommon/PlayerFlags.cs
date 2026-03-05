namespace SpiritReforged.Common.PlayerCommon;

/// <summary> Handles dynamic nullable flags keyed by string. Flags are reset by <see cref="ModPlayer.ResetEffects"/> and <see cref="ModPlayer.UpdateDead"/>. </summary>
public class PlayerFlags : ModPlayer
{
	private readonly Dictionary<string, bool?> Flags = [];

	public override void ResetEffects()
	{
		foreach (string name in Flags.Keys)
			Flags[name] = null;
	}

	public override void UpdateDead() => ResetEffects();
	public override void PostSavePlayer() => Flags.Clear();

	public bool? CheckFlag(string name)
	{
		if (Flags.TryGetValue(name, out bool? flag))
		{
			return flag;
		}
		else
		{
			Flags.Add(name, false);
			return false;
		}
	}

	public void SetFlag(string name, bool? value = true)
	{
		if (Flags.TryGetValue(name, out _))
			Flags[name] = value;
		else
			Flags.Add(name, value);
	}
}