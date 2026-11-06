using System;
using Godot;

public static class MoveTowards
{
    public static float Calculate(
        float current,
        float target,
        float relativeSpeed,
        float minSpeed,
        float delta
    )
    {
        return Calculate(
            current,
            target,
            relativeSpeed,
            minSpeed,
            delta,
            static (x, y) => x - y,
            static (x, y) => x + y,
            static (x, scalar) => x * scalar,
            static x => x
        );
    }

    public static Vector2 Calculate(
        Vector2 current,
        Vector2 target,
        float relativeSpeed,
        float minSpeed,
        float delta
    )
    {
        return Calculate(
            current,
            target,
            relativeSpeed,
            minSpeed,
            delta,
            static (x, y) => x - y,
            static (x, y) => x + y,
            static (x, scalar) => x * scalar,
            static x => x.Length()
        );
    }

    public static Vector3 Calculate(
        Vector3 current,
        Vector3 target,
        float relativeSpeed,
        float minSpeed,
        float delta
    )
    {
        return Calculate(
            current,
            target,
            relativeSpeed,
            minSpeed,
            delta,
            static (x, y) => x - y,
            static (x, y) => x + y,
            static (x, scalar) => x * scalar,
            static x => x.Length()
        );
    }

    private static T Calculate<T>(
        T current,
        T target,
        float relativeSpeed,
        float minSpeed,
        float delta,
        Func<T, T, T> sub,
        Func<T, T, T> add,
        Func<T, float, T> scale,
        Func<T, float> len
    )
    {
        var difference = sub(target, current);
        var distance = len(difference);
        if (distance == 0)
        {
            return target;
        }
        var speed = Mathf.Max(distance * relativeSpeed, minSpeed);
        return add(current, scale(difference, Mathf.Min(speed * delta, distance) / distance));
    }
}
