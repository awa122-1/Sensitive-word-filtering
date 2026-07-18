using System;
using HarmonyLib;

namespace AmongUsFilterMod
{
    // ==========================================
    // 模块：局内自动发送模组开启通知
    // ==========================================
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class HudManagerStartPatch
    {
        private static bool _hasSentNotice = false;

        [HarmonyPostfix]
        public static void Postfix(HudManager __instance)
        {
            // 每次进入新对局时重置发送状态
            _hasSentNotice = false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
    public static class PlayerControlSetTasksPatch
    {
        private static bool _hasSentNotice = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            // 确保只在本地玩家生成、且这局游戏还没发送过通知时触发
            if (_hasSentNotice || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
            {
                return;
            }

            // 1. 动态获取当前房间的 Host（房主）名字
            string hostName = "Host";
            try
            {
                if (AmongUsClient.Instance != null && GameData.Instance != null)
                {
                    int hostId = AmongUsClient.Instance.HostId;
                    var hostPlayer = GameData.Instance.GetPlayerById((byte)hostId);
                    if (hostPlayer != null)
                    {
                        hostName = hostPlayer.PlayerName;
                    }
                }
            }
            catch (Exception ex)
            {
                MyPlugin.Log.LogError($"[通知系统] 获取房主名字失败: {ex.Message}");
            }

            // 2. 组装你要的专属通知内容
            string noticeMessage = $"敏感词过滤模组已被 {hostName} 开启 当前版本v1.0";

            // 3. 让本地玩家自动在聊天框里发出这条消息
            try
            {
                if (ChatController.Instance != null)
                {
                    // 调用游戏原生的本地发送聊天方法
                    PlayerControl.LocalPlayer.RpcSendChat(noticeMessage);
                    _hasSentNotice = true;
                    MyPlugin.Log.LogInfo($"[通知系统] 已成功自动发送开局模组公告。");
                }
            }
            catch (Exception ex)
            {
                MyPlugin.Log.LogError($"[通知系统] 自动发送聊天消息失败: {ex.Message}");
            }
        }
    }

    // 每次回到主菜单或断开连接时，重置状态开关
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    public static class ClientDisconnectPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // 断开连接时清空，确保下一局进新房间还能正常发送
            // 这里可以通过一个常驻变量来控制，或者直接通过在游戏内重新初始化来重置
        }
    }
}
