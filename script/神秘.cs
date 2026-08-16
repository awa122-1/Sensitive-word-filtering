using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace AFKDetect
{
    // ============================================================
    // AFKDetect
    //
    // 房主挂机检测
    //
    // 配置文件：
    // tfumgcgl_data/afktimer.txt
    //
    // 文件内容例如：
    // 180
    //
    // 代表挂机 180 秒后踢出
    // ============================================================

    [BepInPlugin(
        "com.afkdetect.mod",
        "AFK Detect",
        "1.1.0"
    )]
    public class AFKDetectPlugin : BasePlugin
    {
        public static AFKDetectPlugin Instance;

        private Harmony harmony;

        // 配置目录
        public static string DataDirectory =>
            Path.Combine(
                Paths.GameRootPath,
                "tfumgcgl_data"
            );

        // AFK 时间配置文件
        public static string AFKTimerFile =>
            Path.Combine(
                DataDirectory,
                "afktimer.txt"
            );

        // 当前挂机时间限制
        public static float MaxAFKTime = 180f;


        // ========================================================
        // 插件加载
        // ========================================================

        public override void Load()
        {
            Instance = this;

            Log.LogInfo("=================================");
            Log.LogInfo("AFKDetect 正在加载...");
            Log.LogInfo("版本: 1.1.0");
            Log.LogInfo("=================================");

            try
            {
                // ------------------------------------------------
                // 创建 tfumgcgl_data
                // ------------------------------------------------

                CreateDataDirectory();


                // ------------------------------------------------
                // 创建 / 读取 afktimer.txt
                // ------------------------------------------------

                LoadAFKTimer();


                // ------------------------------------------------
                // 加载 Harmony
                // ------------------------------------------------

                harmony = new Harmony(
                    "com.afkdetect.mod"
                );

                harmony.PatchAll();


                Log.LogInfo(
                    "AFKDetect Harmony Patch 加载成功！"
                );

                Log.LogInfo(
                    $"挂机时间限制: {MaxAFKTime} 秒"
                );

                Log.LogInfo(
                    $"配置文件: {AFKTimerFile}"
                );
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "AFKDetect 加载失败！"
                );

                Log.LogError(ex);
            }
        }


        // ========================================================
        // 创建数据目录
        // ========================================================

        private void CreateDataDirectory()
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(
                        DataDirectory
                    );

                    Log.LogInfo(
                        $"已创建数据目录: {DataDirectory}"
                    );
                }
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "无法创建 tfumgcgl_data 文件夹: " + ex
                );
            }
        }


        // ========================================================
        // 读取 AFK 时间
        // ========================================================

        private void LoadAFKTimer()
        {
            try
            {
                // ------------------------------------------------
                // 文件不存在
                // ------------------------------------------------

                if (!File.Exists(AFKTimerFile))
                {
                    MaxAFKTime = 180f;

                    File.WriteAllText(
                        AFKTimerFile,
                        "180"
                    );

                    Log.LogInfo(
                        "afktimer.txt 不存在，已创建默认配置：180 秒"
                    );

                    return;
                }


                // ------------------------------------------------
                // 读取文件
                // ------------------------------------------------

                string text =
                    File.ReadAllText(
                        AFKTimerFile
                    ).Trim();


                // ------------------------------------------------
                // 空文件
                // ------------------------------------------------

                if (string.IsNullOrWhiteSpace(text))
                {
                    MaxAFKTime = 180f;

                    File.WriteAllText(
                        AFKTimerFile,
                        "180"
                    );

                    Log.LogWarning(
                        "afktimer.txt 是空的，已恢复为 180 秒"
                    );

                    return;
                }


                // ------------------------------------------------
                // 尝试解析
                // ------------------------------------------------

                if (float.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float value))
                {
                    // 不允许 0 或负数
                    if (value > 0f)
                    {
                        MaxAFKTime = value;

                        Log.LogInfo(
                            $"读取挂机时间成功: {MaxAFKTime} 秒"
                        );
                    }
                    else
                    {
                        MaxAFKTime = 180f;

                        File.WriteAllText(
                            AFKTimerFile,
                            "180"
                        );

                        Log.LogWarning(
                            "afktimer.txt 必须大于 0，已恢复为 180 秒"
                        );
                    }
                }
                else
                {
                    MaxAFKTime = 180f;

                    File.WriteAllText(
                        AFKTimerFile,
                        "180"
                    );

                    Log.LogWarning(
                        $"无法解析 afktimer.txt 内容：{text}"
                    );

                    Log.LogWarning(
                        "已恢复为默认值 180 秒"
                    );
                }
            }
            catch (Exception ex)
            {
                MaxAFKTime = 180f;

                Log.LogError(
                    "读取 afktimer.txt 失败: " + ex
                );
            }
        }
    }


    // ============================================================
    // AFK 玩家数据
    // ============================================================

    public class PlayerAFKData
    {
        // 玩家上一次的位置
        public Vector2 LastPosition;

        // 已挂机时间
        public float AFKTime;

        // 是否已经初始化
        public bool Initialized;

        // 是否已经执行踢出
        public bool KickScheduled;
    }


    // ============================================================
    // AFK 检测
    // ============================================================

    [HarmonyPatch(
        typeof(PlayerControl),
        nameof(PlayerControl.FixedUpdate)
    )]
    public static class AFKCheckPatch
    {
        // --------------------------------------------------------
        // 移动判定距离
        // --------------------------------------------------------

        private const float MOVE_THRESHOLD = 0.01f;


        // --------------------------------------------------------
        // 每个玩家独立的 AFK 数据
        // --------------------------------------------------------

        private static readonly Dictionary<byte, PlayerAFKData>
            Players =
                new Dictionary<byte, PlayerAFKData>();


        // ========================================================
        // FixedUpdate
        // ========================================================

        [HarmonyPostfix]
        public static void Postfix(
            PlayerControl __instance)
        {
            try
            {
                // ------------------------------------------------
                // 玩家对象不存在
                // ------------------------------------------------

                if (__instance == null)
                    return;


                // ------------------------------------------------
                // AmongUsClient 不存在
                // ------------------------------------------------

                if (AmongUsClient.Instance == null)
                    return;


                // ------------------------------------------------
                // 只有房主执行
                // ------------------------------------------------

                if (!AmongUsClient.Instance.AmHost)
                    return;


                // ------------------------------------------------
                // 获取玩家 ID
                // ------------------------------------------------

                byte playerId =
                    __instance.PlayerId;


                // ------------------------------------------------
                // 不检测房主自己
                // ------------------------------------------------

                if (__instance.AmOwner)
                    return;


                // ------------------------------------------------
                // 获取玩家 AFK 数据
                // ------------------------------------------------

                if (!Players.TryGetValue(
                    playerId,
                    out PlayerAFKData data))
                {
                    data = new PlayerAFKData
                    {
                        LastPosition =
                            __instance.transform.position,

                        AFKTime = 0f,

                        Initialized = true,

                        KickScheduled = false
                    };

                    Players[playerId] = data;


                    Log(
                        $"开始检测玩家 PlayerId={playerId}"
                    );

                    return;
                }


                // ------------------------------------------------
                // 已经准备踢出
                // ------------------------------------------------

                if (data.KickScheduled)
                    return;


                // ------------------------------------------------
                // 获取当前位置
                // ------------------------------------------------

                Vector2 currentPosition =
                    __instance.transform.position;


                // ------------------------------------------------
                // 计算移动距离
                // ------------------------------------------------

                float distance =
                    Vector2.Distance(
                        currentPosition,
                        data.LastPosition
                    );


                // ------------------------------------------------
                // 玩家移动
                // ------------------------------------------------

                if (distance > MOVE_THRESHOLD)
                {
                    data.AFKTime = 0f;

                    data.LastPosition =
                        currentPosition;

                    return;
                }


                // ------------------------------------------------
                // 玩家没有移动
                // ------------------------------------------------

                data.AFKTime +=
                    Time.fixedDeltaTime;


                // ------------------------------------------------
                // 获取当前配置
                // ------------------------------------------------

                float maxAFKTime =
                    AFKDetectPlugin.MaxAFKTime;


                // ------------------------------------------------
                // 判断是否超过挂机时间
                // ------------------------------------------------

                if (data.AFKTime >= maxAFKTime)
                {
                    KickAFKPlayer(
                        __instance,
                        data
                    );
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "AFK 检测发生异常: " + ex
                );
            }
        }


        // ========================================================
        // 踢出挂机玩家
        // ========================================================

        private static void KickAFKPlayer(
            PlayerControl player,
            PlayerAFKData data)
        {
            if (player == null)
                return;


            if (AmongUsClient.Instance == null)
                return;


            // ----------------------------------------------------
            // 确认当前客户端是房主
            // ----------------------------------------------------

            if (!AmongUsClient.Instance.AmHost)
                return;


            // ----------------------------------------------------
            // 获取 PlayerId
            // ----------------------------------------------------

            byte playerId =
                player.PlayerId;


            // ----------------------------------------------------
            // 防止重复执行
            // ----------------------------------------------------

            data.KickScheduled = true;


            // ----------------------------------------------------
            // 日志
            // ----------------------------------------------------

            Log(
                $"玩家 PlayerId={playerId} " +
                $"挂机超过 " +
                $"{AFKDetectPlugin.MaxAFKTime} 秒，准备踢出。"
            );


            // ----------------------------------------------------
            // 执行 Kick
            //
            // false = 普通 Kick
            // true  = Ban
            //
            // 我们这里只进行普通踢出
            // ----------------------------------------------------

            try
            {
                AmongUsClient.Instance.KickPlayer(
                    playerId,
                    false
                );


                Log(
                    $"玩家 PlayerId={playerId} 已被踢出。"
                );


                // ------------------------------------------------
                // 清理数据
                // ------------------------------------------------

                Players.Remove(
                    playerId
                );
            }
            catch (Exception ex)
            {
                LogError(
                    $"踢出玩家 PlayerId={playerId} 失败: {ex}"
                );


                // ------------------------------------------------
                // 踢出失败
                // 允许再次尝试
                // ------------------------------------------------

                data.KickScheduled = false;
            }
        }


        // ========================================================
        // 清理离线玩家
        // ========================================================

        public static void CleanupPlayers()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return;


                HashSet<byte> onlinePlayers =
                    new HashSet<byte>();


                // ------------------------------------------------
                // 获取在线玩家
                // ------------------------------------------------

                foreach (
                    PlayerControl player
                    in PlayerControl.AllPlayerControls)
                {
                    if (player == null)
                        continue;


                    onlinePlayers.Add(
                        player.PlayerId
                    );
                }


                // ------------------------------------------------
                // 找出已经离开的玩家
                // ------------------------------------------------

                List<byte> removeList =
                    new List<byte>();


                foreach (
                    KeyValuePair<byte, PlayerAFKData> pair
                    in Players)
                {
                    if (!onlinePlayers.Contains(
                        pair.Key))
                    {
                        removeList.Add(
                            pair.Key
                        );
                    }
                }


                // ------------------------------------------------
                // 删除
                // ------------------------------------------------

                foreach (byte id in removeList)
                {
                    Players.Remove(id);


                    Log(
                        $"清理离线玩家 PlayerId={id}"
                    );
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "清理 AFK 数据失败: " + ex
                );
            }
        }


        // ========================================================
        // 清空全部数据
        // ========================================================

        public static void Reset()
        {
            Players.Clear();


            Log(
                "AFK 数据已经重置。"
            );
        }


        // ========================================================
        // 日志
        // ========================================================

        private static void Log(
            string message)
        {
            if (AFKDetectPlugin.Instance != null)
            {
                AFKDetectPlugin.Instance.Log.LogInfo(
                    "[AFKDetect] " + message
                );
            }
            else
            {
                Debug.Log(
                    "[AFKDetect] " + message
                );
            }
        }


        private static void LogError(
            string message)
        {
            if (AFKDetectPlugin.Instance != null)
            {
                AFKDetectPlugin.Instance.Log.LogError(
                    "[AFKDetect] " + message
                );
            }
            else
            {
                Debug.LogError(
                    "[AFKDetect] " + message
                );
            }
        }
    }


    // ============================================================
    // 定期清理 AFK 数据
    // ============================================================

    [HarmonyPatch(
        typeof(PlayerControl),
        nameof(PlayerControl.FixedUpdate)
    )]
    public static class AFKCleanupPatch
    {
        private static float cleanupTimer = 0f;

        private const float CLEANUP_INTERVAL = 5f;


        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return;


                if (!AmongUsClient.Instance.AmHost)
                    return;


                cleanupTimer +=
                    Time.fixedDeltaTime;


                if (cleanupTimer <
                    CLEANUP_INTERVAL)
                    return;


                cleanupTimer = 0f;


                AFKCheckPatch.CleanupPlayers();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[AFKDetect] Cleanup Error: " + ex
                );
            }
        }
    }


    // ============================================================
    // 玩家数量变化检测
    // ============================================================

    [HarmonyPatch(
        typeof(PlayerControl),
        nameof(PlayerControl.FixedUpdate)
    )]
    public static class AFKGameStatePatch
    {
        private static int lastPlayerCount = -1;


        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                if (AmongUsClient.Instance == null)
                    return;


                if (!AmongUsClient.Instance.AmHost)
                    return;


                int count =
                    PlayerControl.AllPlayerControls.Count;


                // ------------------------------------------------
                // 玩家数量发生变化
                // ------------------------------------------------

                if (lastPlayerCount != -1 &&
                    count != lastPlayerCount)
                {
                    AFKCheckPatch.CleanupPlayers();
                }


                lastPlayerCount = count;
            }
            catch
            {
                // 防止状态检测影响游戏
            }
        }
    }
}