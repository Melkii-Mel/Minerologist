using Godot;

public sealed class DirAccess
{
    private readonly Directory _directory;

    private DirAccess(Directory directory)
    {
        _directory = directory;
    }

    public static DirAccess? Open(string path)
    {
        var directory = new Directory();

        if (directory.Open(path) != Error.Ok)
            return null;

        return new DirAccess(directory);
    }

    public string[] GetFiles()
    {
        return GetEntries(isDirectory: false);
    }

    public string[] GetDirectories()
    {
        return GetEntries(isDirectory: true);
    }

    private string[] GetEntries(bool isDirectory)
    {
        var result = new Godot.Collections.Array<string>();

        _directory.ListDirBegin(true, true);

        string entry;

        while ((entry = _directory.GetNext()) != "")
        {
            if (_directory.CurrentIsDir() == isDirectory)
                result.Add(entry);
        }

        _directory.ListDirEnd();

        return [.. result];
    }
}
