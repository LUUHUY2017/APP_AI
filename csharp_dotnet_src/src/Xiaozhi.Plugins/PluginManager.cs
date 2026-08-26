using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xiaozhi.Plugins;

public interface IPlugin
{
    string Name { get; }
    Task InitializeAsync();
    Task ShutdownAsync();
}

public class PluginManager
{
    private readonly List<IPlugin> _plugins = new();

    public void RegisterPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
    }

    public async Task InitializeAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            await plugin.InitializeAsync();
        }
    }

    public async Task ShutdownAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            await plugin.ShutdownAsync();
        }
    }
}
