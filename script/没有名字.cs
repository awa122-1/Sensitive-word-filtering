using System;
using AmongUsFilterMod.Config;
using AmongUsFilterMod.Utils;
using UnityEngine;

namespace AmongUsFilterMod.CursorCustom
{
    public static class CursorManager
    {
        public static void SetCursor()
        {
#if Windows
            try
            {
                // 加载 Cursor.png
                Sprite sprite = ResourceLoader.LoadSprite("Cursor.png");

                // 如果配置项已开启且 sprite 存在，使用 sprite.texture；否则传入 null 还原系统默认指针
                Texture2D texture = (ConfigManager.UseModCursor != null && ConfigManager.UseModCursor.Value && sprite != null) 
                    ? sprite.texture 
                    : null;

                // 强制应用指针
                Cursor.SetCursor(
                    texture,
                    Vector2.zero,
                    CursorMode.Auto
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CursorManager] 设置指针异常: {ex}");

                // 发生异常时回滚开关，防止反复出错
                if (ConfigManager.UseModCursor != null)
                {
                    ConfigManager.UseModCursor.Value = false;
                }
            }
#endif
        }
    }
}