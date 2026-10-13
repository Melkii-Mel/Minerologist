using System;
using System.Collections.Concurrent;
using Godot;

public class MeshContainer : Spatial, IMeshContainer
{
    private readonly ConcurrentQueue<Action> _actions = new();

    public void Defer(Action action)
    {
        _actions.Enqueue(action);
        CallDeferred(nameof(ProcessDeferred));
    }

    private void ProcessDeferred()
    {
        while (_actions.TryDequeue(out var action))
            action();
    }
}
