using SpiritReforged.Common.Multiplayer;
using System.IO;

namespace SpiritReforged.Common.DebuffOverhaul;

public sealed class BuffDetours : ILoadable
{
	internal class SyncExtensionData : PacketData
	{
		private readonly int _npcIndex;

		public SyncExtensionData() { }
		public SyncExtensionData(int npcIndex) => _npcIndex = npcIndex;

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			int npcIndex = reader.ReadInt32(); //Read the npc index to sync
			NPC npc = Main.npc[npcIndex];

			if (npc.TryGetGlobalNPC(out ExtendedBuffGlobalNPC globalNPC))
			{
				globalNPC.buffByType.Clear();
				byte count = reader.ReadByte(); //Read the number of extensions

				for (int c = 0; c < count; c++)
				{
					ushort type = reader.ReadUInt16(); //Read the buff type
					if (BuffExtension.BuffHandler.FromType(type, npc) is BuffExtension b)
					{
						globalNPC.buffByType.Add(type, b);
						globalNPC.buffByType[type].NetReceive(reader); //Read the associated buff data
					}
				}
			}
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_npcIndex); //Write the npc index to sync
			NPC npc = Main.npc[_npcIndex];

			if (npc.TryGetGlobalNPC(out ExtendedBuffGlobalNPC globalNPC))
			{
				modPacket.Write((byte)globalNPC.buffByType.Count); //Write the number of extensions
				foreach (int type in globalNPC.buffByType.Keys)
				{
					modPacket.Write((ushort)type); //Write the buff type
					globalNPC.buffByType[type].NetSend(modPacket); //Write the associated buff data
				}
			}
		}
	}

	/// <summary> Whether combat text caused by damage over time should be prevented. </summary>
	public static bool BlockDoTText { get; set; }

    public void Load(Mod mod)
    {
        On_NPC.AddBuff += AddExtension; //NPC hooks
        On_NPC.DelBuff += DelExtension;
		On_NPC.UpdateNPC_BuffApplyVFX += DisableVFX;
		//On_NPC.UpdateNPC_BuffSetFlags += SetExtensionFlags;

		HealthBarHook.PostDrawHealthBar += DrawExtensionHealthBars;

        //Handle DoT combat text
        On_CombatText.NewText_Rectangle_Color_string_bool_bool += DisableDoT;
        On_NPC.UpdateNPC_BuffApplyDOTs += static (orig, self) =>
        {
            orig(self);
            BlockDoTText = false; //Reset to default
        };
    }

	private static void DrawExtensionHealthBars(HealthBarHook.Options options, Entity entity)
    {
        if (entity is NPC npc && npc.TryGetGlobalNPC<ExtendedBuffGlobalNPC>(out var global))
		{
			foreach (int type in global.buffByType.Keys)
			{
				global.buffByType[type].PostDrawHealthBar(Main.spriteBatch, npc, options);
				break;
			}
		}
    }

	/*private static void SetExtensionFlags(On_NPC.orig_UpdateNPC_BuffSetFlags orig, NPC self, bool lowerBuffTime)
	{
		orig(self, lowerBuffTime);

		for (int i = 0; i < NPC.maxBuffs; i++)
		{
			int type = self.buffType[i];

			if (type != 0 && self.TryGetGlobalNPC(out ExtendedBuffGlobalNPC global))
				global.AddExtension(self, type); //Activate buff extensions when necessary
		}
	}*/

	private static void AddExtension(On_NPC.orig_AddBuff orig, NPC self, int type, int time, bool quiet)
    {
		if (self.TryGetGlobalNPC(out ExtendedBuffGlobalNPC global))
			global.AddExtension(self, type);

		orig(self, type, time, quiet);

		if (!quiet && Main.netMode == NetmodeID.Server)
			new SyncExtensionData(self.whoAmI).Send();
	}

    private static void DelExtension(On_NPC.orig_DelBuff orig, NPC self, int buffIndex)
    {
        int type = self.buffType[buffIndex];
        orig(self, buffIndex);

		if (self.TryGetGlobalNPC(out ExtendedBuffGlobalNPC global))
			global.RemoveExtension(self, type);
	}

	private static void DisableVFX(On_NPC.orig_UpdateNPC_BuffApplyVFX orig, NPC self)
	{
		bool doDefault = true;
		if (self.TryGetGlobalNPC(out ExtendedBuffGlobalNPC global))
		{
			foreach (int type in global.buffByType.Keys)
			{
				BuffExtension b = global.buffByType[type];

				b.DoVisuals();
				doDefault |= !b.UsesCustomVFX;
			}
		}

		if (doDefault)
			orig(self); //Skip orig
	}

	private static int DisableDoT(On_CombatText.orig_NewText_Rectangle_Color_string_bool_bool orig, Rectangle location, Color color, string text, bool dramatic, bool dot)
	{
		int value = orig(location, color, text, dramatic, dot);

		if (dot && BlockDoTText)
			Main.combatText[value].active = false;

		return value;
	}

	public void Unload() { }
}

public sealed class ExtendedBuffGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    /// <summary> Buff extension data indexed by buff ID. </summary>
    public readonly Dictionary<int, BuffExtension> buffByType = [];

	public void AddExtension(NPC npc, int type)
	{
		if (npc.buffImmune[type])
			return;

		if (buffByType.TryGetValue(type, out BuffExtension extension)) //The buff extension is already present, reapply
		{
			extension.ApplyTo(npc, true);
		}
		else if (BuffExtension.BuffHandler.FromType(type) is BuffExtension b) //The buff extension is not present, newly apply
		{
			buffByType.Add(type, b);
			buffByType[type].ApplyTo(npc, false);
		}
	}

	public void RemoveExtension(NPC npc, int type) => buffByType.Remove(type);

	public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
		List<int> queuedForRemoval = [];
        foreach (int type in buffByType.Keys)
		{
			BuffExtension extension = buffByType[type];

			if (!npc.HasBuff(extension.Type))
				queuedForRemoval.Add(type); //Remove (if BuffDetours.DelExtension does not catch it)
			else
				extension.UpdateLifeRegen(ref damage); //Update life regen
		}

		foreach (int type in queuedForRemoval)
			buffByType.Remove(type); //Remove all queued buff extensions

	}
}