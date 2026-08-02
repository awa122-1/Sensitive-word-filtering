using System;

using HarmonyLib;

using UnityEngine;

using TMPro;

using Il2CppSystem.Collections.Generic;



namespace AmongUsFilterMod

{

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]

    public static class HudManagerUpdateTaskPatch

    {

        private static float _lastRefreshTime = 0f;



        [HarmonyPostfix]

        public static void Postfix(HudManager __instance)

        {

            // 确保场上有玩家数据

            if (PlayerControl.AllPlayerControls == null) return;



            int totalTasks = 0;

            int completedTasks = 0;

            var allPlayers = PlayerControl.AllPlayerControls;



            // 1. 每帧计算全局进度（带顶级防崩溃保护）

            for (int i = 0; i < allPlayers.Count; i++)

            {

                var playerCtrl = allPlayers[i];

                if (playerCtrl == null || playerCtrl.Data == null) continue;



                try

                {

                    var playerInfo = playerCtrl.Data;

                    if (playerInfo.Disconnected || playerInfo.Role == null || playerInfo.Role.IsImpostor) continue;



                    List<NetworkedPlayerInfo.TaskInfo> taskList = playerInfo.Tasks;

                    if (taskList == null) continue;



                    for (int j = 0; j < taskList.Count; j++)

                    {

                        var task = taskList[j];

                        if (task != null)

                        {

                            totalTasks++;

                            if ((bool)task.Complete) completedTasks++;

                        }

                    }

                }

                catch {}

            }



            // 更新左上角全局进度条

            if (totalTasks > 0 && GameData.Instance != null)

            {

                try

                {

                    GameData.Instance.TotalTasks = totalTasks;

                    GameData.Instance.CompletedTasks = completedTasks;

                }

                catch {}

            }



            // 2. 提高刷新频率（0.2秒一刷），强行覆盖游戏原版渲染

            float curTime = Time.time;

            if (curTime - _lastRefreshTime > 0.2f)

            {

                _lastRefreshTime = curTime;

                for (int i = 0; i < allPlayers.Count; i++)

                {

                    var playerCtrl = allPlayers[i];

                    if (playerCtrl != null)

                    {

                        ForceUpdatePlayerNameText(playerCtrl);

                    }

                }

            }

        }



        // 🎯 核心改变：不管任务是不是0，不管有没有分配，直接强行刷文本组件！

        private static void ForceUpdatePlayerNameText(PlayerControl playerCtrl)

        {

            if (playerCtrl == null || playerCtrl.Data == null) return;



            try

            {

                var playerInfo = playerCtrl.Data;

                if (playerInfo.Disconnected) return;



                // 如果是内鬼，我们不显示任务进度

                if (playerInfo.Role != null && playerInfo.Role.IsImpostor) return;



                int pTotal = 0;

                int pCompleted = 0;



                // 尝试抓取任务

                List<NetworkedPlayerInfo.TaskInfo> taskList = playerInfo.Tasks;

                if (taskList != null)

                {

                    for (int j = 0; j < taskList.Count; j++)

                    {

                        var task = taskList[j];

                        if (task != null)

                        {

                            pTotal++;

                            if ((bool)task.Complete) pCompleted++;

                        }

                    }

                }



                // 即使当前任务算出来是 0，我们也强行显示 [0/0] 或者保持原样，而不是直接 return 退出！

                string rawName = playerInfo.PlayerName ?? "Player";

                string colorHex = (pTotal > 0 && pCompleted == pTotal) ? "#00FF00" : ((pCompleted > 0) ? "#FF8C00" : "#FF0000");

                

                // 拼接最终头顶文字

                string finalNameText = pTotal > 0 

                    ? $"{rawName} <color={colorHex}>[{pCompleted}/{pTotal}]</color>"

                    : $"{rawName} <color=#FF0000>[0/0]</color>";



                // 强制覆写 TMP 文本

                var tmPros = playerCtrl.GetComponentsInChildren<TextMeshPro>(true);

                if (tmPros != null)

                {

                    for (int k = 0; k < tmPros.Length; k++)

                    {

                        if (tmPros[k] != null) tmPros[k].text = finalNameText;

                    }

                }



                // 强制覆写 UGUI 文本

                var tmProUguis = playerCtrl.GetComponentsInChildren<TextMeshProUGUI>(true);

                if (tmProUguis != null)

                {

                    for (int k = 0; k < tmProUguis.Length; k++)

                    {

                        if (tmProUguis[k] != null) tmProUguis[k].text = finalNameText;

                    }

                }

            }

            catch {}

        }

    }

} 

