using System;
using Godot;

public class MuseumDiggable : Node
{
    private readonly string _diggableName;
    private readonly MeshInstance _placeholder;

    public MuseumDiggable(string diggableName, MeshInstance placeholder)
    {
        _diggableName = diggableName;
        _placeholder = placeholder;
    }

    // TODO:
    public void InceraseNumber()
    {
        throw new NotImplementedException();
    }
}
