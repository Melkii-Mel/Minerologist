using System;
using System.Collections.Generic;
using Godot;

public static class SoundPlayer
{
    private static readonly List<AudioPlayer> _players = [];
    private static readonly List<int> _freePlayers = [];

    private static Node? _parent;

    public static void Init(Node parent)
    {
        _parent = parent;
    }

    public static int Play(AudioStream stream, bool loop = false)
    {
        var idx = ConsumeOrCreatePlayer();
        var player = _players[idx];
        player.Stream = stream;
        player.Play(loop);
        return idx;
    }

    public static void StopPlaying(int idx)
    {
        AudioPlayer player = _players[idx];
        if (player.Playing)
        {
            player.Stop();
            FreePlayer(idx);
        }
    }

    private static int ConsumeOrCreatePlayer()
    {
        var i = _freePlayers.Count - 1;

        if (i == -1)
        {
            return CreatePlayer(false);
        }

        var idx = _freePlayers[i];
        _freePlayers.RemoveAt(i);

        return idx;
    }

    private static int CreatePlayer(bool free)
    {
        var idx = _players.Count;

        AudioPlayer player = new();
        _players.Add(player);

        player.PlaybackFinished += () => FreePlayer(idx);

        var parent =
            _parent
            ?? throw new InvalidOperationException("Sound player must be initialized before usage");
        parent.AddChild(player);

        if (free)
        {
            _freePlayers.Add(idx);
        }

        return idx;
    }

    private static void FreePlayer(int idx)
    {
        _freePlayers.Add(idx);
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
