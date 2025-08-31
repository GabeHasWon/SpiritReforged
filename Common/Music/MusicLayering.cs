using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Common.Music;

internal class MusicLayering : ModSystem
{
	public class MusicLayer(IMusicLayer layer)
	{
		public IMusicLayer Layer = layer;
		public float Fade = 0f;
	}

	public List<MusicLayer> Layers = [];

	public override void Load() => On_LegacyAudioSystem.UpdateAudioEngine += AddLayer;
	public override void Unload() => Layers.Clear();
	public override void PostSetupContent() => Layers = [.. ModContent.GetContent<IMusicLayer>().Select(x => new MusicLayer(x))];

	private void AddLayer(On_LegacyAudioSystem.orig_UpdateAudioEngine orig, LegacyAudioSystem self)
	{
		if (Main.gameMenu)
		{
			orig(self);
			return;
		}

		foreach (MusicLayer layer in Layers)
			UpdateSingleSlot(self, layer);

		orig(self);
	}

	private static void UpdateSingleSlot(LegacyAudioSystem self, MusicLayer layer)
	{
		const float FadeSpeed = 0.005f;

		int slot = layer.Layer.MusicSlot;

		if (layer.Layer.CanPlay(ref layer.Fade))
			layer.Fade += FadeSpeed;
		else
			layer.Fade -= FadeSpeed;

		layer.Fade = MathHelper.Clamp(layer.Fade, 0, 1);
		Main.musicFade[slot] = layer.Fade;

		if (layer.Fade > 0 && self.WaveBank.IsPrepared)
		{
			IAudioTrack track = self.AudioTracks[slot];

			if (!track.IsPlaying)
				if (self.MusicReplayDelay == 0)
				{
					if (Main.SettingMusicReplayDelayEnabled)
						self.MusicReplayDelay = Main.rand.Next(14400, 21601);

					track.Reuse();
					track.SetVariable(MusicID.Sets.SkipsVolumeRemap[slot] ? "VolumeDirect" : "Volume", layer.Fade);
					track.Play();
				}
			else
				track.SetVariable(MusicID.Sets.SkipsVolumeRemap[slot] ? "VolumeDirect" : "Volume", layer.Fade);
		}
	}
}
