using System;
using Godot;

public class SessionResizer
{
    public static void ResizeAllSessions(SessionContainer[] _sessions, Node node)
    {
        var screenSize = ScreenSize(node);
        int count = _sessions.Length;
        for (var i = 0; i < count; i++)
        {
            var session = _sessions[i];
            var (relativePosition, relativeSize) = (
                RelativeSessionContainerPosition(i),
                RelativeSessionContainerSize(i, count)
            );
            var (position, size) = (relativePosition * screenSize, relativeSize * screenSize);
            session.SetPositionAndSize(position, size);
        }
    }

    private static Vector2 ScreenSize(Node node)
    {
        return node.GetViewport().GetVisibleRect().Size;
    }

    private static Vector2 RelativeSessionContainerPosition(int i)
    {
        return i switch
        {
            0 => new(0, 0),
            1 => new(0.5f, 0),
            2 => new(0, 0.5f),
            3 => new(0.5f, 0.5f),
            _ => throw new NotSupportedException(
                "Does not support more than 4 screens in parallel"
            ),
        };
    }

    private static Vector2 RelativeSessionContainerSize(int i, int count)
    {
        if (count > 4)
        {
            throw _screensNotSupported;
        }
        return (count, i) switch
        {
            (1, _) => new(1, 1),
            (2, _) => new(0.5f, 1),
            (3, 2) => new(1, 0.5f),
            _ => new(0.5f, 0.5f),
        };
    }

    private static readonly NotSupportedException _screensNotSupported = new(
        "Does not support more than 4 screens in parallel"
    );
}
