Vegetation Remover
一个轻量的 SPT 插件，用来移除地图上的草地、树木/大型植被和灌木/沼泽碰撞。进图后按快捷键即可实时切换。

✨ 功能
Ctrl + Numpad1	开/关 草地	关闭 GPUInstancer 的 Detail 管理器（草的实际渲染管线）；找不到时走 Terrain.detailObjectDensity
Ctrl + Numpad2	开/关 树木 / 大型植被	按地图名匹配对应的 GameObject 层级
Ctrl + Numpad3	开/关 灌木 / 沼泽碰撞	关闭 filbert / fibert 灌木及 Swamp 沼泽的碰撞体
按一次关，再按一次恢复。所有状态在切图后自动重置。

下载 VegetationRemover.zip

放到 BepInEx/plugins

🔧 从源码构建
已安装的 SPT，路径需在 VegetationRemover.csproj 的 <TarkovDir> 里配好（默认 D:\SPT-5.0.0-47242-BE\）

构建
bash
# Windows
build.bat

# 或手动
dotnet build VegetationRemover.csproj -c Release
构建成功后会自动拷贝 VegetationRemover.dll 到 BepInEx/plugins/VegetationRemover/。

项目结构
text
VegetationRemover/
├── VegetationRemover.csproj              # 项目文件
├── VegetationRemoverPlugin.cs            # BasePlugin 入口
├── VegetationRemoverPatch.cs             # Harmony patch，挂在 GameWorld.OnGameStarted
├── VegetationRemoverController.cs        # 主逻辑（快捷键 + 状态机 + 缓存）
└── build.bat                             # 一键编译脚本
🧠 实现细节
状态机：Update 里跑 4 个状态 —— 等待 GameWorld → 等待 MainPlayer → 延迟 1.5s 稳定 → 接受快捷键

缓存优化：草 / 树 / 碰撞的查找结果只做一次并缓存，按键时仅切换 enabled / SetActive，避免每次查找卡顿

反射匹配：使用 GetIl2CppType().Name.Contains(...) 字符串匹配而非强类型引用，跨 SPT 版本更稳

路径兜底：草优先禁用 GPUInstancerDetailManager，找不到时退化到把 Terrain.detailObjectDensity = 0

树木匹配规则参考了 CWX-MegaMod 的做法（地图资源命名是硬约束），在此致谢。

⚠️ 免责声明
本插件仅供 SPT 使用

🙏 致谢
BepInEx — 插件框架

Harmony — 运行时补丁

CWX-MegaMod — 树木/灌木的资源命名匹配规则参考
