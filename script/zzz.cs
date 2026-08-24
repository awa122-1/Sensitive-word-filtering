using System;
using AmongUsFilterMod.CustomCursor;
using AmongUsFilterMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod.ModStampPatch
{
    [HarmonyPatch(typeof(ModManager))]
    public static class ModManagerPatch
    {
        [HarmonyPatch(nameof(ModManager.ShowModStamp))]
        [HarmonyPostfix]
        public static void ShowModStamp_Postfix()
        {
            try
            {
                // 1. 触发光标设置
                CursorManager.SetCursor();

                // 2. 查找并替换 ModStamp 贴图
                GameObject modStamp = GameObject.Find("ModStamp");

                if (modStamp == null)
                {
                    // 容错搜索：遍历当前场景
                    foreach (GameObject rootObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        Transform found = rootObj.transform.Find("ModStamp");
                        if (found != null)
                        {
                            modStamp = found.gameObject;
                            break;
                        }
                    }
                }

                if (modStamp == null)
                {
                    Debug.LogWarning("[ModStampPatch] 场景中未找到 ModStamp GameObject");
                    return;
                }

                // 调整大小与贴图
                modStamp.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                SpriteRenderer renderer = modStamp.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    // 从注册管理器加载资源
                    Sprite newStamp = ResourceLoader.GetSprite("hi_qwq_ms.png", 100f);
                    if (newStamp != null)
                    {
                        renderer.sprite = newStamp;
                        Debug.Log("[ModStampPatch] ModStamp 贴图替换成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModStampPatch] Patch 过程出现异常: {ex}");
            }
        }
    }
}