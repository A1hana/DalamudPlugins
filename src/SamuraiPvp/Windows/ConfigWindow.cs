using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SamuraiPvp.Core;
using SamuraiPvp.Interop;

namespace SamuraiPvp.Windows;

internal sealed class ConfigWindow(Configuration configuration, TargetController controller,
    ZantetsukenProtector protector, Action save) : Window("武士 · 斩铁剑助手###SamuraiPvpConfig")
{
    public override void OnOpen() => Size = new Vector2(540, 480);

    public override void Draw()
    {
        ImGui.TextUnformatted("连续选人 · 手动斩铁剑");
        ImGui.Separator();
        var changed = false;
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("启用插件（仅武士 PvP）", ref enabled)) { configuration.Enabled = enabled; changed = true; }
        var auto = configuration.AutoSelect;
        if (ImGui.Checkbox("LB 就绪时自动选择预计斩杀人数最多的目标", ref auto)) { configuration.AutoSelect = auto; changed = true; }
        ImGui.TextWrapped("自动选人会替换手动目标。人数相同优先保留当前目标；不使用镜头锁定，不自动施放任何技能。");
        var protect = configuration.ProtectLb;
        if (ImGui.Checkbox("斩铁剑防误触", ref protect)) { configuration.ProtectLb = protect; changed = true; }
        ImGui.TextWrapped("LB 未就绪，或实际施法目标不符合崩破、护盾、免疫及施法范围条件时，阻止斩铁剑请求。");
        var hud = configuration.ShowHud;
        if (ImGui.Checkbox("显示人数悬浮窗", ref hud)) { configuration.ShowHud = hud; changed = true; }
        var locked = configuration.LockHud;
        if (ImGui.Checkbox("固定悬浮窗位置（鼠标穿透）", ref locked)) { configuration.LockHud = locked; changed = true; }
        var interval = configuration.ScanIntervalMs;
        if (ImGui.SliderInt("扫描间隔", ref interval, 16, 250, "%d ms")) { configuration.ScanIntervalMs = interval; changed = true; }
        var remaining = configuration.MinimumKuzushiSeconds;
        if (ImGui.SliderFloat("崩破剩余时间余量", ref remaining, 0, 1, "%.2f 秒"))
        {
            configuration.MinimumKuzushiSeconds = remaining;
            changed = true;
        }
        ImGui.TextWrapped("仅统计由自己施加的崩破，剩余时间必须大于上述余量。护盾用客户端百分比估算，HP＋护盾不超过最大 HP 才纳入预测。防御不会被排除。");
        ImGui.Separator();
        ImGui.TextUnformatted($"状态：{controller.Display.Message}");
        ImGui.TextUnformatted($"保护接口：{(protector.Available ? "正常" : "不可用，请查看 Dalamud 日志")}");
        ImGui.TextUnformatted($"当前目标预计 {controller.Display.CurrentKills} 人 · 最佳目标预计 {controller.Display.BestKills} 人");
        ImGui.TextWrapped("人数是当前快照估计。服务器延迟、移动、回血、护盾和状态变化均可能影响最终结果。");
        if (changed) save();
    }
}
