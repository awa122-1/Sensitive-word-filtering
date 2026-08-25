using AmongUsFilterMod.Config;
using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;

namespace AmongUsFilterMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class ModMain : BasePlugin
    {
        public const string PluginGuid = "com.tfu.amongusfiltermod";
        public const string PluginName = "AmongUsFilterMod";
        public const string PluginVersion = "1.0.0";

        public Harmony Harmony { get; } = new Harmony(PluginGuid);

        public override void Load()
        {
            // 1. 初始化 Config 选项
            ConfigManager.Init(Config);

            // 2. 挂载所有 Patch (ModManager.LateUpdate 等)
            Harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} 成功装载并应用 Patch！");
        }
    }
}