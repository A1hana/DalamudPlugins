using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SamuraiPvp;

internal sealed record Services(
    IDalamudPluginInterface PluginInterface,
    ICommandManager Commands,
    IFramework Framework,
    IClientState ClientState,
    IObjectTable Objects,
    ITargetManager Targets,
    IGameInteropProvider Interop,
    IPluginLog Log);
