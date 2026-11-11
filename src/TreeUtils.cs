using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

public static class TreeUtils
{
    // Type

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
        foreach (var child in Traverse(node))
        {
            if (child is T typedChild)
                yield return typedChild;
        }
    }

    // Exact name

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
        foreach (var child in Traverse(node))
        {
            if (child.Name == name)
                yield return child;
        }
    }

    public static IEnumerable<T> GetChildrenOfTypeByName<T>(this Node node, string name)
        where T : class
    {
        foreach (var child in Traverse(node))
        {
            if (child.Name == name && child is T typedChild)
                yield return typedChild;
        }
    }

    // Regex name matching

    public static Node? GetChildByNameRegex(
        this Node node,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
    {
        foreach (var child in GetChildrenByNameRegex(node, pattern, options))
            return child;

        return null;
    }

    public static T? GetChildOfTypeByNameRegex<T>(
        this Node node,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
        where T : class
    {
        foreach (var child in GetChildrenOfTypeByNameRegex<T>(node, pattern, options))
            return child;

        return null;
    }

    public static IEnumerable<Node> GetChildrenByNameRegex(
        this Node node,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
    {
        var regex = new Regex(pattern, options);

        foreach (var child in Traverse(node))
        {
            if (regex.IsMatch(child.Name))
                yield return child;
        }
    }

    public static IEnumerable<T> GetChildrenOfTypeByNameRegex<T>(
        this Node node,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
        where T : class
    {
        var regex = new Regex(pattern, options);

        foreach (var child in Traverse(node))
        {
            if (regex.IsMatch(child.Name) && child is T typedChild)
                yield return typedChild;
        }
    }

    // Internal traversal

    private static IEnumerable<Node> Traverse(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            yield return child;

            foreach (var descendant in Traverse(child))
                yield return descendant;
        }
    }

    // Tree representation

    public static string GetTreeString(Node node, Func<Node, string>? map = null, int depth = 0)
    {
        var sb = new StringBuilder();

        sb.AppendLine(new string(' ', depth * 2) + (map != null ? map(node) : node.Name));

        foreach (Node child in node.GetChildren())
            sb.Append(GetTreeString(child, map, depth + 1));

        return sb.ToString();
    }
}
