using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Common.PrimitiveRendering;

public sealed class TrailDetours : ILoadable
{
	public void Load(Mod mod)
	{
		On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += Projectile_NewProjectile;
		On_Main.DrawCachedProjs += Main_DrawCachedProjs;
		On_Main.DrawProjectiles += Main_DrawProjectiles;
	}

	public void Unload() { }

	private static void Main_DrawCachedProjs(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
	{
		if (!Main.dedServ && projCache == Main.instance.DrawCacheProjsBehindNPCs)
			AssetLoader.VertexTrailManager.DrawTrails(Main.spriteBatch, TrailLayer.UnderCachedProjsBehindNPC);

		orig(self, projCache, startSpriteBatch);
	}

	private static void Main_DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
	{
		if (!Main.dedServ)
			AssetLoader.VertexTrailManager.DrawTrails(Main.spriteBatch, TrailLayer.UnderProjectile);

		orig(self);

		if (!Main.dedServ)
			AssetLoader.VertexTrailManager.DrawTrails(Main.spriteBatch, TrailLayer.AboveProjectile);
	}

	private static int Projectile_NewProjectile(On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float orig, IEntitySource source, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2)
	{
		int index = orig(source, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2);
		Projectile projectile = Main.projectile[index];

		if (projectile.ModProjectile is ITrailProjectile)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				(projectile.ModProjectile as ITrailProjectile).DoTrailCreation(AssetLoader.VertexTrailManager);
			else
				new SpawnTrailData(index).Send();
		}

		return index;
	}
}

internal class SpawnTrailData : PacketData
{
	public SpawnTrailData() { }
	public SpawnTrailData(int index) => _index = index;

	private readonly int _index;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		int index = reader.ReadInt32();

		if (Main.netMode == NetmodeID.Server)
		{
			new SpawnTrailData(index).Send();
			return;
		}

		if (Main.projectile[index].ModProjectile is IManualTrailProjectile trailProj)
			trailProj.DoTrailCreation(AssetLoader.VertexTrailManager);
	}

	public override void OnSend(ModPacket modPacket) => modPacket.Write(_index);
}