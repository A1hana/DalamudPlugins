using Dalamud.Configuration;

namespace SamuraiPvp;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool AutoSelect { get; set; } = true;
    public bool ProtectLb { get; set; } = true;
    public bool ShowHud { get; set; } = true;
    public bool LockHud { get; set; }
    public int ScanIntervalMs { get; set; } = 33;
    public float MinimumKuzushiSeconds { get; set; } = 0.15f;

    internal void Normalize()
    {
        ScanIntervalMs = Math.Clamp(ScanIntervalMs, 16, 250);
        MinimumKuzushiSeconds = float.IsFinite(MinimumKuzushiSeconds)
            ? Math.Clamp(MinimumKuzushiSeconds, 0, 1) : 0.15f;
    }
}
