# Aihana Dalamud Plugins

FF14 Dalamud 插件仓库，目前提供 **Samurai PvP · 斩铁剑助手**（API 15）。

## 安装

在游戏中打开 **Dalamud 设置 → 实验性 → 自定义插件仓库**，添加并启用以下地址，然后保存：

```text
https://raw.githubusercontent.com/A1hana/DalamudPlugins/main/pluginmaster.json
```

在插件安装器中刷新列表，搜索 **Samurai PvP** 并安装。后续更新通过同一插件源获取。

## 斩铁剑助手

- LB 就绪时，连续选择 5 米范围内预计斩杀人数最多的崩破目标。
- 过滤无敌、不死、受保护、兵器以及血量加护盾超过斩杀线的目标。
- 提供斩铁剑防误触和预计斩杀人数显示。
- 仅武士 PvP 生效，LB 由你手动释放。

输入 `/samlb` 打开设置；`/samlb on`、`/samlb off` 开关插件，`/samlb hud` 切换人数显示。

人数根据客户端状态估算，实际结果可能受延迟、移动、回血和护盾变化影响。

## 源码与构建

源码位于 [`src/SamuraiPvp`](src/SamuraiPvp)，安装包见 [Releases](https://github.com/A1hana/DalamudPlugins/releases)。需要 .NET 10 SDK 和 Dalamud API 15 开发文件：

```powershell
./build.ps1 -DalamudLibPath 'C:\path\to\dalamud-dev'
```

构建生成 `dist/latest.zip` 并更新插件列表。发布时先将 ZIP 上传到脚本给出的 Release 标签，再提交更新后的 `pluginmaster.json`。列表采用数组格式，可继续加入其他插件。

许可证：[AGPL-3.0-or-later](LICENSE.md)。来源声明见 [NOTICE](NOTICE)。
