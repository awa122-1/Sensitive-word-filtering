using System;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace AmongUsFilterMod
{
    // ==========================================
    // 模块：全员任务监控 + 头顶状态三色动态渲染（活着可见）
    // ==========================================

    // 补丁 1：解除官方任务栏延迟刷新限制，强行实时计算真实数据
    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskProgress))]
    public static class RealtimeTaskProgressPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameData __instance)
        {
            int totalTasks = 0;
            int completedTasks = 0;

            foreach (var playerInfo in GameData.Instance.AllPlayers)
            {
                if (playerInfo == null || playerInfo.Disconnected || playerInfo.Role == null || playerInfo.Role.IsImpostor)
                {
                    continue;
                }

                var tasks = playerInfo.Tasks;
                if (tasks == null) continue;

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    totalTasks++;
                    if (task.TaskComplete)
                    {
                        completedTasks++;
                    }
                }
            }

            if (totalTasks > 0)
            {
                GameData.Instance.TotalTasks = totalTasks;
                GameData.Instance.CompletedTasks = completedTasks;
            }

            return false; // 接管官方逻辑
        }
    }

    // 补丁 2：高频驱动刷新，并在每个玩家头顶根据进度动态渲染红、橙、绿三色数字
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdateTaskPatch
    {
        [HarmonyPostfix]
        public static void Postfix(HudManager __instance)
        {
            if (GameData.Instance == null) return;

            // 1. 强行高频刷新左上角全局进度条
            if (__instance.TaskBar != null)
            {
                GameData.Instance.RecomputeTaskProgress();
            }

            // 2. 遍历场上所有玩家，动态计算并上色
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.nameText == null || player.Data == null) continue;

                // 排除内鬼（内鬼不计入正常好人任务变色逻辑）
                if (player.Data.Role != null && player.Data.Role.IsImpostor)
                {
                    continue; 
                }

                var tasks = player.Data.Tasks;
                if (tasks == null || tasks.Count == 0) continue;

                int total = 0;
                int completed = 0;

                for (int i = 0; i < tasks.Count; i++)
                {
                    total++;
                    if (tasks[i].TaskComplete)
                    {
                        completed++;
                    }
                }

                // 拿到玩家原本的名字
                string originalName = player.Data.PlayerName;

                // 核心：动态颜色判定（使用 TextMeshPro 原生支持的 Hex 颜色标签）
                string colorHex;
                if (completed == total)
                {
                    // 1. 全部做完 -> 纯绿色
                    colorHex = "#00FF00";
                }
                else if (completed > 0)
                {
                    // 2. 做了一部分 -> 橙色
                    colorHex = "#FF8C00";
                }
                else
                {
                    // 3. 根本没做 (completed == 0) -> 纯红色
                    colorHex = "#FF0000";
                }

                // 渲染最终带有状态变色后缀的名字标签
                player.nameText.text = $"{originalName} <color={colorHex}>[{completed}/{total}]</color>";
            }
        }
    }
}
