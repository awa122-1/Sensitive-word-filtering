using System;
using System.IO;
using HarmonyLib;

namespace AmongUsFilterMod
{
    public static class AutoFolderInstaller
    {
        // 仅在后台静默创建必需的文件夹，不加任何弹窗或提示
        public static void InitializeFolders()
        {
            try
            {
                string gameRootDir = Directory.GetCurrentDirectory();
                string dataDir = Path.Combine(gameRootDir, "tfumgcgl_data");
                string musicDir = Path.Combine(dataDir, "Music");
                string imagesDir = Path.Combine(dataDir, "Images");

                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                if (!Directory.Exists(musicDir)) Directory.CreateDirectory(musicDir);
                if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError($"[初始化] 创建文件夹异常: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class FolderInstallerInitPatch
    {
        private static bool _hasChecked = false;

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!_hasChecked)
            {
                _hasChecked = true;
                AutoFolderInstaller.InitializeFolders();
            }
        }
    }
}