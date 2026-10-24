using Godot;

public class NodeCounter(Node root)
{
    public int Count()
    {
        return Count(root);
    }

    private int Count(Node root)
    {
        var count = 0;
        var children = root.GetChildren();

        for (var i = 0; i < children.Count; i++)
        {
            var child = (Node)children[i];
            count += 1;
            count += Count(child);
        }

        return count;
    }
}
