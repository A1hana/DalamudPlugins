# 2026 年 10 月 27 日测试版发布说明

**测试版，未实测，可能崩溃。** 两款插件仅完成编译、离线检查和界面检查，没有完成游戏内加载、实际战斗及与其他插件共存的验证。当前版本不承诺功能有效或运行稳定。

## CleaveCompass 群攻罗盘 1.0.0.0

- 估算圆形、扇形和直线群攻覆盖，比较当前目标与最佳目标并提供一键选人。
- 支持短时移动预测、碰撞圈、PvE 防误拉怪、俯视示意图和场景叠加。
- 默认项是几何模板，需要按实际技能设置；技能仍由玩家释放。
- 入口：`/cleave`；选人：`/cleave pick`。
- [测试安装包](packages/CleaveCompass-1.0.0.0-testing.zip) · [完整源码](packages/CleaveCompass-1.0.0.0-testing-source.zip)

## Aihana Tuner 战斗调节器 1.0.0.0

- 独立控制移速倍率、技能射程、受迫位移距离、本地异常状态屏蔽和魅惑恐惧移动拦截。
- 射程倍率与固定加算互斥，同一时间只使用选中的公式；不扩大群攻半径。
- 位移按距离倍率计算，0.8 表示 80%；0 仍调用原函数，但传入零距离，原锁定行为可能保留。
- 状态名单可编辑，带 PvP 控制预设。客户端状态过滤不等于服务器解除状态或保证免疫。
- 默认全部停用；入口：`/atune`；全部停用：`/atune off`。
- [测试安装包](packages/AihanaTuner-1.0.0.0-testing.zip) · [完整源码](packages/AihanaTuner-1.0.0.0-testing-source.zip)

## 获取方式

沿用仓库订阅地址：

`https://raw.githubusercontent.com/A1hana/DalamudPlugins/main/pluginmaster.json`

两款插件使用 Dalamud API 15，标记为仅测试版。搜索不到时开启测试版插件显示后刷新；已有同名开发版时先停用，避免重复加载。

本次上传仅调整发布标记、安装说明和打包内容，DLL 与此前本地交付的 1.0.0.0 相同。校验值见 [TESTING-SHA256SUMS.txt](packages/TESTING-SHA256SUMS.txt)。现有 SamuraiPvp 和 AihanaPvp 条目保持不变。
