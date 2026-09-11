using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SamuraiPvp.Core;
using SamuraiPvp.Interop;

namespace SamuraiPvp.Windows;

internal sealed class HudWindow : Window
{
    private readonly Configuration configuration;
    private readonly TargetController controller;
    private readonly GameState game;

    public HudWindow(Configuration configuration, TargetController controller, GameState game)
        : base("斩铁剑预测###SamuraiPvpHud")
    {
        this.configuration = configuration;
        this.controller = controller;
        this.game = game;
        IsOpen = true;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        Position = new Vector2(50, 300);
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public override bool DrawConditions() => configuration.Enabled && configuration.ShowHud && game.IsSamuraiPvP;

    public override void PreDraw()
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoFocusOnAppearing;
        if (configuration.LockHud) Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs;
    }

    public override void Draw()
    {
        var state = controller.Display;
        var color = state.CurrentKills > 0 ? new Vector4(0.4f, 1f, 0.6f, 1) : new Vector4(1f, 0.75f, 0.4f, 1);
        ImGui.TextColored(color, $"当前目标预计斩杀：{state.CurrentKills} 人");
        ImGui.TextUnformatted($"目标：{state.CurrentName}");
        if (state.Ready)
            ImGui.TextUnformatted($"最佳：{state.BestName} · {state.BestKills} 人");
        ImGui.TextUnformatted(state.Message);
        if (!configuration.ProtectLb) ImGui.TextColored(new Vector4(1, 0.5f, 0.4f, 1), "防误触已关闭");
    }
}
