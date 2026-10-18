using System;
using System.Collections.Concurrent;
using Godot;

public class GodotNodeContainer : Node
{
    private readonly ConcurrentBag<Action> _actions = [];

    protected void DeferOrCall(Action action)
    {
        if (System.Environment.CurrentManagedThreadId == 1)
        {
            action();
        }
        else
        {
            _actions.Add(action);
        }
    }

    public override void _Process(float delta)
    {
        while (_actions.TryTake(out var action))
        {
            action();
        }
    }
}
