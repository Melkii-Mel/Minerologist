using System;
using System.Collections.Generic;
using Godot;

public static class Animations
{
    private static readonly List<(
        AnimationInstance Instance,
        float Completion,
        Action? onFinish
    )> _animations = [];

    public static void Init(Action<Action<float>> bindUpdate)
    {
        bindUpdate(Update);
    }

    public static void EntityFlyTowards(
        Spatial from,
        Spatial to,
        float duration = 1,
        Action? onFinish = null,
        EasingFunction? easingFunction = default
    )
    {
        var animation = new AnimationEntityFlyTowards(from, to);
        AddAnimation(animation, duration, easingFunction, onFinish);
    }

    public static void AddAnimation(
        AnimationInstance animation,
        float duration = 1,
        EasingFunction? easingFunction = default,
        Action? onFinish = null
    )
    {
        easingFunction ??= new EFEaseInOut(3);
        animation.Init(duration, easingFunction);
        _animations.Add((animation, 0, onFinish));
    }

    public static void Update(float delta)
    {
        for (var i = _animations.Count - 1; i >= 0; i--)
        {
            var (instance, completion, onFinish) = _animations[i];
            completion += delta / instance.Duration;
            if (completion <= 1)
            {
                _animations[i] = (instance, completion, onFinish);
                instance.Apply(Mathf.Min(completion, 1));
            }
            else
            {
                RemoveAnimationAt(i);
                onFinish?.Invoke();
            }
        }
    }

    private static void RemoveAnimationAt(int i)
    {
        _animations[i] = _animations[_animations.Count - 1];
        _animations.RemoveAt(_animations.Count - 1);
    }
}

public abstract class AnimationInstance
{
    public float Duration { get; private set; } = -1;
    public EasingFunction EasingFunction { get; private set; } = new EFLinear();

    public bool Initialized { get; private set; } = false;

    public void Init(float duration, EasingFunction easingFunction)
    {
        if (Initialized)
        {
            throw new InvalidOperationException("AnimationInstance cannot be initialized twice");
        }
        Initialized = true;
        Duration = duration;
        EasingFunction = easingFunction;
    }

    public void Apply(float progress)
    {
        ApplyInternal(EasingFunction.Evalueate(progress));
    }

    // completion is the value between 0 and 1 where 0 is the initial state and 1 is the final state of the animation
    protected abstract void ApplyInternal(float progress);
}
