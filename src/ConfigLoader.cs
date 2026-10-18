using System;
using System.IO;
using Tomlyn;

public class ConfigLoader : IDisposable
{
    private readonly string _configPath;
    private readonly Action<Config> _configSetter;
    private readonly FileSystemWatcher _watcher;

    public ConfigLoader(string configPath, Action<Config> configSetter)
    {
        _configPath = Path.GetFullPath(configPath);
        _configSetter = configSetter;

        LoadConfig();

        _watcher = new FileSystemWatcher(
            Path.GetDirectoryName(_configPath),
            Path.GetFileName(_configPath)
        )
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        ListenForFileChanges();
    }

    private void ListenForFileChanges()
    {
        _watcher.Changed += (_, _) => LoadConfig();
        _watcher.Created += (_, _) => LoadConfig();
        _watcher.Renamed += (_, _) => LoadConfig();
    }

    public Config LoadConfig()
    {
        var toml = File.ReadAllText(_configPath);
        var config =
            TomlSerializer.Deserialize<Config>(toml)
            ?? throw new InvalidDataException("Config file cannot represent null");
        _configSetter(config);
        return config;
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
