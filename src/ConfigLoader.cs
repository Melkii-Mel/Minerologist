#define USE_FALLBACK_CONFIG
// #define WRITE_FALLBACK_CONFIG

using System;
using System.IO;
using System.Text.RegularExpressions;
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

        var config = LoadConfig();
        var configConfig = config.ConfigConfig;
        if (configConfig.WriteConfigTemplate)
        {
            CreateConfigTemplate(configConfig.ConfigTemplatePath);
        }
        if (configConfig.WriteMergedConfig)
        {
            CreateMergedConfig(configConfig.MergedConfigPath, config);
        }

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

    public Config LoadConfig()
    {
        Config config;
#if USE_FALLBACK_CONFIG
        if (File.Exists(_configPath))
#else
        if (true)
#endif
        {
            var toml = File.ReadAllText(_configPath);
            config =
                TomlSerializer.Deserialize<Config>(toml)
                ?? throw new InvalidDataException("Config file cannot represent null");
        }
        else
        {
            config = new Config();
#if WRITE_FALLBACK_CONFIG
            WriteConfigToFile(_configPath, config);
#endif
        }
        _configSetter(config);
        config.Initialize();
        return config;
    }

    public void CreateConfigTemplate(string path)
    {
        var config = new Config();
        config.Initialize();
        WriteConfigToFile(path, config);
    }

    public void CreateMergedConfig(string path, Config config)
    {
        WriteConfigToFile(path, config);
    }

    public void WriteConfigToFile(string path, Config config)
    {
        var toml = TomlSerializer.Serialize(
            config,
            new TomlSerializerOptions() { WriteIndented = true, IndentSize = 2 }
        );

        toml = Regex.Replace(toml, @"(?m)(?<!\A)^(\[)", "\n$1");

        toml = Regex.Replace(
            toml,
            @"(?<![\w.])([+-]?\d+\.\d*?[1-9])0{5,}\d*(?:[eE][+-]?\d+)?",
            "$1"
        );

        File.WriteAllText(path, toml);
    }

    private void ListenForFileChanges()
    {
        _watcher.Changed += (_, _) => LoadConfig();
        _watcher.Created += (_, _) => LoadConfig();
        _watcher.Renamed += (_, _) => LoadConfig();
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
