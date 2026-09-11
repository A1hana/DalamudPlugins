using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SamuraiPvp.Core;
using SamuraiPvp.Interop;
using SamuraiPvp.Windows;

namespace SamuraiPvp;

public sealed class Plugin : IDalamudPlugin
{
    private readonly Services services;
    private readonly WindowSystem windows = new("SamuraiPvp");
    private readonly Configuration configuration;
    private readonly ZantetsukenProtector protector;
    private readonly TargetController controller;
    private readonly ConfigWindow configWindow;
    private bool disposed;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commands, IFramework framework,
        IClientState clientState, IObjectTable objects, ITargetManager targets, IGameInteropProvider interop,
        IPluginLog log)
    {
        services = new(pluginInterface, commands, framework, clientState, objects, targets, interop, log);
        configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.Normalize();
        var game = new GameState(services, configuration);
        protector = new(services, configuration, game);
        controller = new(services, configuration, game, protector);
        configWindow = new(configuration, controller, protector, Save);
        windows.AddWindow(configWindow);
        windows.AddWindow(new HudWindow(configuration, controller, game));

        var commandInfo = new CommandInfo(OnCommand)
        {
            HelpMessage = "打开武士斩铁剑助手；on/off 开关；hud 显示开关。仅选人，LB 手动释放。",
        };
        commands.AddHandler("/samlb", commandInfo);
        commands.AddHandler("/samuraipvp", commandInfo);
        framework.Update += OnUpdate;
        pluginInterface.UiBuilder.Draw += windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi += OpenConfig;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
    }

    private void OnUpdate(IFramework _) => controller.Update();
    private void OpenConfig() => configWindow.IsOpen = true;
    private void Save()
    {
        configuration.Normalize();
        services.PluginInterface.SavePluginConfig(configuration);
        // Changes are applied on the next framework update, where object access belongs.
    }

    private void OnCommand(string command, string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "on": configuration.Enabled = true; Save(); break;
            case "off": configuration.Enabled = false; Save(); break;
            case "hud": configuration.ShowHud = !configuration.ShowHud; Save(); break;
            default: configWindow.Toggle(); break;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        services.Framework.Update -= OnUpdate;
        services.PluginInterface.UiBuilder.Draw -= windows.Draw;
        services.PluginInterface.UiBuilder.OpenMainUi -= OpenConfig;
        services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        services.Commands.RemoveHandler("/samlb");
        services.Commands.RemoveHandler("/samuraipvp");
        protector.Dispose();
        controller.Reset("已卸载");
        windows.RemoveAllWindows();
    }
}
