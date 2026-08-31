using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class SafeWatermarkPatch
    {
        private static bool _hasRun = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_hasRun) return;
            _hasRun = true;

            try
            {
                // 定位至 Among Us.exe 根目录下的 tfumgcgl_data/Images/hi_qwq_ms.png
                string gameRootDir = Directory.GetCurrentDirectory();
                string imgPath = Path.Combine(gameRootDir, "tfumgcgl_data", "Images", "hi_qwq_ms.png");

                if (!File.Exists(imgPath))
                {
                    MyPlugin.Log?.LogWarning($"[水印] 没找到图片，请检查路径: {imgPath}");
                    return;
                }

                byte[] imgBytes = File.ReadAllBytes(imgPath);
                Texture2D tex = new Texture2D(2, 2);

                if (ImageConversion.LoadImage(tex, imgBytes))
                {
                    Sprite customSprite = Sprite.Create(
                        tex, 
                        new Rect(0, 0, tex.width, tex.height), 
                        new Vector2(0.5f, 0.5f), 
                        100f
                    );

                    ModManager modManager = ModManager.Instance;
                    if (modManager != null && modManager.ModStamp != null)
                    {
                        modManager.ModStamp.sprite = customSprite;
                        modManager.ModStamp.enabled = true;

                        // 缩放大小设置
                        float targetScale = 0.125f; 
                        modManager.ModStamp.transform.localScale = new Vector3(targetScale, targetScale, 1f);

                        MyPlugin.Log?.LogInfo("[水印] 替换并调整大小成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                MyPlugin.Log?.LogError($"[水印] 异常已截获: {ex.Message}");
            }
        }
    }
}