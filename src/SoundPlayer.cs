using System;
using System.Collections.Generic;
using Godot;

public static class SoundPlayer
{
    private static readonly List<AudioPlayer> _players = [];
    private static readonly List<AudioPlayer> _freePlayers = [];

    private static Node? _parent;

    public static void Init(Node parent)
    {
        _parent = parent;
    }

    public static AudioPlayer Play(AudioStream stream, bool loop = false)
    {
        var player = ConsumeOrCreatePlayer();
        player.Stream = stream;
        player.Play(loop);
        return player;
    }

    public static void StopPlaying(AudioPlayer player)
    {
        if (player.Playing)
        {
            player.Stop();
            FreePlayer(player);
        }
    }

    private static AudioPlayer ConsumeOrCreatePlayer()
    {
        var i = _freePlayers.Count - 1;

        if (i == -1)
        {
            return CreatePlayer(false);
        }

        var player = _freePlayers[i];
        _freePlayers.RemoveAt(i);

        return player;
    }

    private static AudioPlayer CreatePlayer(bool free)
    {
        AudioPlayer player = new();
        _players.Add(player);

        player.PlaybackFinished += () => FreePlayer(player);

        var parent =
            _parent
            ?? throw new InvalidOperationException("Sound player must be initialized before usage");
        parent.AddChild(player);

        if (free)
        {
            _freePlayers.Add(player);
        }

        return player;
    }

    private static void FreePlayer(AudioPlayer player)
    {
        _freePlayers.Add(player);
    }
}

public class AudioPlayer : AudioStreamPlayer
{
    public event Action? PlaybackFinished;

    private bool _looping = false;

    public AudioPlayer()
    {
        Connect("finished", this, nameof(OnPlaybackFinished));
    }

    public void Play(bool loop)
    {
        _looping = loop;
        Play();
    }

    public void OnPlaybackFinished()
    {
        if (_looping)
        {
            Play(true);
        }
        else
        {
            PlaybackFinished?.Invoke();
        }
    }
}
