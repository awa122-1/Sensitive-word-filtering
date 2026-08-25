using AmongUsFilterMod.CursorCustom;
using AmongUsFilterMod.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmongUsFilterMod.Patches
{
    [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
    internal class ModManagerLateUpdatePatch
    {
        private static bool _firstRun = true;
        private static string _lastScene = "";
        private static Sprite _cachedStampSprite;

        [HarmonyPrefix]
        public static void Prefix(ModManager __instance)
        {
            // 1. 触发原版 ShowModStamp
            __instance.ShowModStamp();

            if (!_firstRun)
            {
                // 场景切换逻辑判定
                string currentScene = SceneManager.GetActiveScene().name;
                if (_lastScene != currentScene)
                {
                    var last = _lastScene;
                    _lastScene = currentScene;

                    // 切换场景时再次尝试补全光标
                    CursorManager.SetCursor();

                    if (last != "SplashIntro")
                    {
                        OnSceneChange(_lastScene);
                    }
                }
            }
            else
            {
                // 2. 首次运行：加载自定义 ModStamp.png 贴图并替换
                if (_cachedStampSprite == null)
                {
                    _cachedStampSprite = ResourceLoader.LoadSprite("ModStamp.png", 100f);
                }

                if (_cachedStampSprite != null && __instance.ModStamp != null)
                {
                    __instance.ModStamp.sprite = _cachedStampSprite;
                }

                // 3. 应用鼠标指针
                CursorManager.SetCursor();
                _firstRun = false;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ModManager __instance)
        {
            if (__instance == null || __instance.ModStamp == null || __instance.localCamera == null)
                return;

            // 4. 精确计算并保持 ModStamp 处于屏幕右上角位置
            var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
            __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                __instance.localCamera,
                AspectPosition.EdgeAlignments.RightTop,
                new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f)
            );

            // 设置显示缩放
            __instance.ModStamp.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }

        private static void OnSceneChange(string newSceneName)
        {
            Debug.Log($"[ModManagerPatch] 切换场景至: {newSceneName}");
        }
    }
}