using System;
using System.Collections.Generic;
using System.Text;
using Godot;

public static class TreeUtils
{
    public static T? GetChildOfType<T>(this Node node)
        where T : class
    {
        foreach (var child in GetChildrenOfType<T>(node))
            return child;

        return null;
    }

    public static IEnumerable<T> GetChildrenOfType<T>(this Node node)
        where T : class
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in GetChildrenOfType<T>(child))
                yield return descendant;
        }
    }

    public static Node? GetChildByName(this Node node, string name)
    {
        foreach (var child in GetChildrenByName(node, name))
            return child;

        return null;
    }

    public static T? GetChildOfTypeByName<T>(this Node node, string name)
        where T : class
    {
        foreach (var child in GetChildrenOfTypeByName<T>(node, name))
            return child;

        return null;
    }

    public static IEnumerable<Node> GetChildrenByName(this Node node, string name)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child.Name == name)
                yield return child;

            foreach (var descendant in GetChildrenByName(child, name))
                yield return descendant;
        }
    }

    public static IEnumerable<T> GetChildrenOfTypeByName<T>(this Node node, string name)
        where T : class
    {
        foreach (Node child in node.GetChildren())
        {
            if (child.Name == name && child is T typedChild)
                yield return typedChild;

            foreach (var descendant in GetChildrenOfTypeByName<T>(child, name))
                yield return descendant;
        }
    }

    public static string GetTreeString(Node node, Func<Node, string>? map = null, int depth = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string(' ', depth * 2) + (map != null ? map(node) : node.Name));

        foreach (Node child in node.GetChildren())
        {
            sb.Append(GetTreeString(child, map, depth + 1));
        }

        return sb.ToString();
    }
}
