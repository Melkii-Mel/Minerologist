using System;

public class ConfigLoader
{
    private readonly string _configPath;
    private readonly Action<Config> _configSetter;

    public ConfigLoader(string configPath, Action<Config> configSetter)
    {
        _configPath = configPath;
        _configSetter = configSetter;
        LoadConfig();
        ListenForFileChanges();
    }

    private void ListenForFileChanges()
    {
        throw new NotImplementedException();
    }

    public Config LoadConfig()
    {
        throw new NotImplementedException();
    }
}
