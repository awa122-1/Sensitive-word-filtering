using System;
using HarmonyLib;

namespace AmongUsFilterMod
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    public static class SendChatPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ChatController __instance, PlayerControl sourcePlayer, ref string chatText, bool censor)
        {
            if (sourcePlayer == null || string.IsNullOrWhiteSpace(chatText)) return true;

            if (MyPlugin.FilterInstance != null && MyPlugin.FilterInstance.ContainsSensitiveWord(chatText))
            {
                // 获取名字
                string playerName = "Unknown";
                try { playerName = sourcePlayer.Data?.PlayerName ?? sourcePlayer.name; } catch {}

                MyPlugin.Log.LogWarning($"[警告] 玩家 {playerName} 发送了违规词汇！");

                // 如果你是房主，且说话的不是你自己，直接执行“天罚”
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    if (PlayerControl.LocalPlayer != null && sourcePlayer.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        MyPlugin.Log.LogError($"[天罚] 正在将违规玩家 {playerName} 踢出房间...");
                        try
                        {
                            // 调用官方大厅踢人接口 (false代表Kick，true代表Ban)
                            // 注意：不同版本参数可能略有差异，这里使用最通用的 ClientId 踢人
                            AmongUsClient.Instance.KickPlayer(sourcePlayer.OwnerId, false);
                        }
                        catch (Exception ex)
                        {
                            MyPlugin.Log.LogError($"自动踢人失败，请手动在列表踢出: {ex.Message}");
                        }
                    }
                }

                // 房主本地屏幕依然拦截，不让这句脏话脏了你的眼
                return false; 
            }

            return true; 
        }
    }
}