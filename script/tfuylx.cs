using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace TFUYlx
{
    [BepInPlugin(
        "fuchan.tfuylx",
        "TFUYlx",
        "1.0.0"
    )]
    [BepInProcess("Among Us.exe")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            // 检查 other2.dll
            string otherPath = Path.Combine(
                Paths.PluginPath,
                "other2.dll"
            );

            if (!File.Exists(otherPath))
            {
                Log.LogError(
                    "[TFUYlx] 未找到 other2.dll，停止运行！"
                );

                return;
            }

            Log.LogInfo(
                "[TFUYlx] 检测到 other2.dll，正常启动。"
            );

            StartMod();
        }

        private void StartMod()
        {
            // TFUYlx 的功能
        }
    }
}