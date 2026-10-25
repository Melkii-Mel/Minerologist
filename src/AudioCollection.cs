using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class AudioCollection
{
    public static Dictionary<string, List<AudioStream>> Sounds { get; } = [];
    public static Dictionary<string, AudioStream> Music { get; } = [];
    public static Dictionary<string, AudioPlayer> ActiveMusic { get; } = [];

    public static string[] SupportedFormats = [".mp3", ".wav", ".ogg"];

    private static readonly Random _rng = new();

    public static void Init()
    {
        var audioManifest = Manifest.Instance.Dir("Audio") ?? throw AudioLoadingException;
        var soundsManifest = audioManifest.Dir("Sounds") ?? throw AudioLoadingException;
        var musicManifest = audioManifest.Dir("Music") ?? throw AudioLoadingException;
        foreach (
            var file in musicManifest.RecursiveFiles().Where(f => SupportedFormats.Contains(f.Ext))
        )
        {
            var name = file.PathRelativeTo(musicManifest, withoutExtension: true);
            Music[name] = GD.Load<AudioStream>("res://" + file.FullPath);
        }
        foreach (var dir in soundsManifest.SubDirs)
        {
            var sounds = new List<AudioStream>();
            Sounds[dir.Name] = sounds;
            GD.Print("Loaded sounds for: ", dir.Name);
            foreach (var file in dir.RecursiveFiles().Where(f => SupportedFormats.Contains(f.Ext)))
            {
                sounds.Add(GD.Load<AudioStream>("res://" + file.FullPath));
            }
        }
    }

    public static AudioStream? RandomSound(string id, Random? rng = default)
    {
        if (!Sounds.ContainsKey(id))
        {
            GD.Print($"Audio `{id}` not found");
            return null;
        }
        return RandomUtils.ChooseRandomItem(Sounds[id], rng ?? _rng);
    }

    public static AudioPlayer PlayRandomSound(string id, Random? rng = default)
    {
        var sound = RandomSound(id, rng);
        return SoundPlayer.Play(sound);
    }

    public static void StopPlayingSound(AudioPlayer? player)
    {
        SoundPlayer.StopPlaying(player);
    }

    public static AudioStream GetMusic(string id)
    {
        return Music[id];
    }

    public static void PlayMusic(string id)
    {
        var player = SoundPlayer.Play(GetMusic(id), loop: true);
        ActiveMusic[id] = player;
    }

    public static void StopMusic(string id)
    {
        SoundPlayer.StopPlaying(ActiveMusic[id]);
        ActiveMusic.Remove(id);
    }

    private static readonly InvalidOperationException AudioLoadingException = new(
        "Failed to load audio"
    );
}
