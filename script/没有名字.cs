using System;
using AmongUsFilterMod.Utils;
using UnityEngine;

namespace AmongUsFilterMod.CustomCursor
{
    public static class CursorManager
    {
        public static void SetCursor()
        {
            try
            {
                // 调用注册器获取 Texture
                Texture2D cursorTex = ResourceLoader.GetTexture("Cursor.png");

                if (cursorTex == null)
                {
                    Debug.LogError("[CursorManager] Cursor.png 注册失败！");
                    return;
                }

                // 强制应用 Unity 指针
                UnityEngine.Cursor.SetCursor(
                    cursorTex,
                    Vector2.zero, // Hotspot (点击热点坐标：左上角)
                    CursorMode.Auto
                );

                Debug.Log("[CursorManager] 自定义 Cursor 设置成功！");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CursorManager] 设置 Cursor 异常: {ex}");
            }
        }
    }
}