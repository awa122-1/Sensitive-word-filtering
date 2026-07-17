using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod
{
    // ==========================================
    // 模块：原版水印自动完美替换（PPU 设定为超精致缩小版）
    // ==========================================
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    public static class ModManagerLateUpdatePatch
    {
        private static bool _firstRun = true;
        private static Sprite _customIconSprite = null;

        [HarmonyPrefix]
        public static void Prefix(ModManager __instance)
        {
            // 让游戏正常走它原生的摆放和显示逻辑
            __instance.ShowModStamp();

            if (_firstRun)
            {
                _customIconSprite = TryLoadLocalLogo();
                _firstRun = false;
            }

            // 自动替换的核心：如果找到了你的专属图片，无情替换掉官方的 Sprite 贴图
            if (_customIconSprite != null && __instance.ModStamp != null)
            {
                __instance.ModStamp.sprite = _customIconSprite;
            }
        }

        // 核心：读取你放在 BepInEx/plugins 目录下的 logo.png 文件
        private static Sprite TryLoadLocalLogo()
        {
            try
            {
                string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string imagePath = Path.Combine(assemblyFolder, "hi_qwq_ms.png");

                if (File.Exists(imagePath))
                {
                    byte[] fileData = File.ReadAllBytes(imagePath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(fileData))
                    {
                        MyPlugin.Log.LogInfo("【水印系统】成功找到原版水印，已将 PPU 调大实现精致缩小效果！");
                        
                        // 【缩小尺寸微调】：
                        // 100f 是官方默认大小。
                        // 150f 会让图标整体缩小。如果觉得还不够小，可以手动改成 180f 或者 200f！
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 350f);
                    }
                }
                else
                {
                    MyPlugin.Log.LogWarning($"【水印系统】未找到 logo.png，将保持官方默认无模组状态样式。");
                }
            }
            catch (Exception ex)
            {
                MyPlugin.Log.LogError($"【水印系统】替换自制图标时发生异常: {ex.Message}");
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPriority(Priority.First)]
    public static class TitleLogoPatch
    {
        public static void Postfix(MainMenuManager __instance)
        {
            GameObject modStampObj = GameObject.Find("ModStamp");
            if (modStampObj == null) return;
        }
    }
}