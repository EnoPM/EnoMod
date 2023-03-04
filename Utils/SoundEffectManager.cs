using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EnoMod.Utils;

public static class SoundEffectManager
{
    public static Dictionary<string, AudioClip> SoundEffects;

    public static void Load()
    {
        SoundEffects = new Dictionary<string, AudioClip>();
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        foreach (var resourceName in resourceNames)
        {
            if (resourceName.Contains($"{nameof(EnoMod)}.Resources.SoundEffects") && resourceName.EndsWith(".raw"))
            {
                SoundEffects.Add(resourceName, Resources.LoadAudioClipFromResources(resourceName)!);
            }
        }
    }

    public static AudioClip? Get(string path)
    {
        // Convenience: As as SoundEffects are stored in the same folder, allow using just the name as well
        if (!path.Contains('.')) path = $"{nameof(EnoMod)}.Resources.SoundEffects.{path}.raw";
        return SoundEffects.TryGetValue(path, out var returnValue) ? returnValue : null;
    }

    public static void Play(string path, float volume = 0.8f)
    {
        var clip = Get(path);
        if (clip == null) return;
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(clip, false, volume);
    }

    public static void Stop(string path)
    {
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.StopSound(Get(path));
    }

    public static void StopAll()
    {
        foreach (var path in SoundEffects.Keys) Stop(path);
    }
}
