using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace EnoMod.Modules;

public static class Helpers
{
    public static readonly Dictionary<string, Sprite> CachedSprites = new();

    public static void Log(string text)
    {
        System.Console.WriteLine(text);
    }

    public static Sprite? LoadSpriteFromResources(string path, float pixelsPerUnit)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            var texture = LoadTextureFromResources(path);
            if (texture == null) return null;
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            System.Console.WriteLine("Error loading sprite from path: " + path);
        }

        return null;
    }

    public static unsafe Texture2D? LoadTextureFromResources(string path)
    {
        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(path);
            if (stream == null) return texture;
            var length = stream.Length;
            var byteTexture = new Il2CppStructArray<byte>(length);
            var read = stream.Read(new Span<byte>(
                IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(),
                (int) length));
            if (read <= 0) return null;
            ImageConversion.LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from resources: " + path);
        }

        return null;
    }

    public static AudioClip? LoadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
    {
        // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity® to export)
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(path);
            if (stream != null)
            {
                var byteAudio = new byte[stream.Length];
                _ = stream.Read(byteAudio, 0, (int) stream.Length);
                var samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
                int offset;
                for (var i = 0; i < samples.Length; i++)
                {
                    offset = i * 4;
                    samples[i] = (float) BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
                }

                var channels = 2;
                var sampleRate = 48000;
                var audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            }
        }
        catch
        {
            System.Console.WriteLine("Error loading AudioClip from resources: " + path);
        }

        return null;

        /* Usage example:
        AudioClip exampleClip = Helpers.loadAudioClipFromResources("TheOtherRoles.Resources.exampleClip.raw");
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        */
    }

    public static Color HexColor(string hexColor, int alpha = 255)
    {
        if (hexColor.IndexOf('#') != -1)
            hexColor = hexColor.Replace("#", string.Empty);

        var red = 0;
        var green = 0;
        var blue = 0;

        switch (hexColor.Length)
        {
            case 6:
                red = int.Parse(hexColor.AsSpan(0, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                green = int.Parse(hexColor.AsSpan(2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                blue = int.Parse(hexColor.AsSpan(4, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                break;
            case 3:
                red = int.Parse(
                    hexColor[0] + hexColor[0].ToString(),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture);
                green = int.Parse(
                    hexColor[1] + hexColor[1].ToString(),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture);
                blue = int.Parse(
                    hexColor[2] + hexColor[2].ToString(),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture);
                break;
        }

        return new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
    }

    public static Color BlendColor(List<Color> clrArr)
    {
        var r = 0f;
        var g = 0f;
        var b = 0f;
        foreach (var color in clrArr)
        {
            r += color.r;
            g += color.g;
            b += color.b;
        }

        r /= clrArr.Count;
        g /= clrArr.Count;
        b /= clrArr.Count;
        return new Color(r, g, b);
    }
}
