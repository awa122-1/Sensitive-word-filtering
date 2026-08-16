using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AmongUsFilterMod.ShowTaskMenu
{
    // =========================================================
    // ShowTaskMenu
    // Insert = 开关任务菜单
    // =========================================================

    public static class ShowTaskMenu
    {
        public static bool Enabled = false;

        private static TasksUI _ui;

        // -----------------------------------------------------
        // 开关菜单
        // -----------------------------------------------------

        public static void Toggle()
        {
            Enabled = !Enabled;

            if (_ui == null)
            {
                CreateUI();
            }

            if (_ui != null)
            {
                _ui.enabled = Enabled;
            }
        }

        // -----------------------------------------------------
        // 创建 UI
        // -----------------------------------------------------

        public static void CreateUI()
        {
            if (_ui != null)
                return;

            var obj = new GameObject("ShowTaskMenu");

            UnityEngine.Object.DontDestroyOnLoad(obj);

            _ui = obj.AddComponent<TasksUI>();
            _ui.enabled = Enabled;
        }
    }


    // =========================================================
    // Insert 按键
    // 使用 HudManager.Update
    // =========================================================

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class ShowTaskMenuKeyPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PlayerControl.LocalPlayer == null)
                return;

            if (Input.GetKeyDown(KeyCode.Insert))
            {
                ShowTaskMenu.Toggle();
            }
        }
    }


    // =========================================================
    // Task UI
    // =========================================================

    public class TasksUI : MonoBehaviour
    {
        public static int windowHeight = 500;
        public static int windowWidth = 600;

        private Rect _windowRect;

        private Vector2 _scrollPosition = Vector2.zero;

        private GUIStyle _playerHeaderStyle;
        private GUIStyle _taskStyle;

        private Il2CppSystem.Text.StringBuilder _tasksString =
            new Il2CppSystem.Text.StringBuilder();

        private readonly Dictionary<string, bool> _expandedPlayers =
            new Dictionary<string, bool>();


        // =====================================================
        // 初始化
        // =====================================================

        private void Start()
        {
            _windowRect = new Rect(
                Screen.width / 2f - windowWidth / 2f,
                Screen.height / 2f - windowHeight / 2f,
                windowWidth,
                windowHeight
            );

            CreateStyles();
        }


        // =====================================================
        // 创建 GUI Style
        // =====================================================

        private void CreateStyles()
        {
            if (GUI.skin == null)
                return;

            _playerHeaderStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };

            _taskStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };
        }


        // =====================================================
        // GUI
        // =====================================================

        private void OnGUI()
        {
            if (!ShowTaskMenu.Enabled)
                return;

            if (PlayerControl.LocalPlayer == null)
                return;

            if (_playerHeaderStyle == null ||
                _taskStyle == null)
            {
                CreateStyles();
            }

            // -------------------------------------------------
            // 显式转换成 GUI.WindowFunction
            // 解决 CS1503
            // -------------------------------------------------

           _windowRect = GUI.Window(
    99991,
    _windowRect,
    (GUI.WindowFunction)DrawWindow,
    "Show Task Menu"
        );
        }

        // =====================================================
        // Window
        // =====================================================

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            DrawTopButtons();

            GUILayout.Space(5);

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                false,
                true
            );

            DrawPlayers();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            // -------------------------------------------------
            // 拖动窗口
            // -------------------------------------------------

            GUI.DragWindow(
                new Rect(
                    0,
                    0,
                    10000,
                    25
                )
            );
        }


        // =====================================================
        // 顶部按钮
        // =====================================================

        private void DrawTopButtons()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(
                "Show Task Menu",
                GUILayout.Width(180)
            );

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "Close",
                    GUILayout.Width(80)
                ))
            {
                ShowTaskMenu.Enabled = false;
                enabled = false;
            }

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            GUILayout.Label(
                "Insert = Open / Close",
                GUILayout.Width(180)
            );

            if (GUILayout.Button(
                    "Complete My Tasks"
                ))
            {
                CompleteMyTasks();
            }

            GUILayout.EndHorizontal();
        }


        // =====================================================
        // 玩家列表
        // =====================================================

        private void DrawPlayers()
        {
            if (PlayerControl.AllPlayerControls == null)
                return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null)
                    continue;

                if (player.Data == null)
                    continue;

                if (string.IsNullOrEmpty(player.Data.PlayerName))
                    continue;

                DrawPlayer(player);
            }
        }


        // =====================================================
        // 单个玩家
        // =====================================================

        private void DrawPlayer(PlayerControl player)
        {
            string playerName =
                player.Data.PlayerName;

            bool expanded;

            _expandedPlayers.TryGetValue(
                playerName,
                out expanded
            );

            string arrow =
                expanded
                    ? "▼"
                    : "▶";


            // -------------------------------------------------
            // 任务数量
            // -------------------------------------------------

            int taskCount = 0;

            if (player.myTasks != null)
            {
                taskCount = player.myTasks.Count;
            }


            // -------------------------------------------------
            // 已完成数量
            // -------------------------------------------------

            int completeCount = 0;

            if (player.myTasks != null)
            {
                completeCount = player.myTasks
                    .ToArray()
                    .Count(t =>
                        t != null &&
                        t.IsComplete
                    );
            }


            // -------------------------------------------------
            // 玩家按钮
            // -------------------------------------------------

            string buttonText =
                $"{arrow} [{completeCount}/{taskCount}] {playerName}";


            if (GUILayout.Button(
                    buttonText,
                    _playerHeaderStyle
                ))
            {
                _expandedPlayers[playerName] =
                    !expanded;
            }


            // -------------------------------------------------
            // 没展开
            // -------------------------------------------------

            if (!expanded)
                return;


            GUILayout.BeginVertical(
                GUI.skin.box
            );


            // =================================================
            // 玩家任务
            // =================================================

            if (player.myTasks != null)
            {
                foreach (var task in player.myTasks)
                {
                    if (task == null)
                        continue;


                    // -----------------------------------------
                    // 排除破坏任务
                    // -----------------------------------------

                    if (IsSabotageTask(task))
                        continue;


                    _tasksString.Clear();


                    // -----------------------------------------
                    // 获取任务文字
                    // -----------------------------------------

                    try
                    {
                        task.AppendTaskText(
                            _tasksString
                        );
                    }
                    catch
                    {
                        continue;
                    }


                    string taskText =
                        _tasksString.ToString();


                    if (string.IsNullOrEmpty(taskText))
                        continue;


                    // -----------------------------------------
                    // 过滤系统任务
                    // -----------------------------------------

                    if (taskText.Contains(
                            "You're dead"))
                    {
                        continue;
                    }


                    if (taskText.Contains(
                            "Sabotage and kill"))
                    {
                        continue;
                    }


                    // -----------------------------------------
                    // 清理颜色标签
                    // -----------------------------------------

                    taskText =
                        taskText
                            .Replace("\n", "")
                            .Replace(
                                "</color>",
                                ""
                            )
                            .Replace(
                                "<color=#00DD00FF>",
                                ""
                            )
                            .Replace(
                                "<color=#FFFF00FF>",
                                ""
                            );


                    // -----------------------------------------
                    // 任务行
                    // -----------------------------------------

                    GUILayout.BeginHorizontal();


                    GUILayout.Label(
                        taskText,
                        _taskStyle
                    );


                    GUILayout.FlexibleSpace();


                    // -----------------------------------------
                    // 完成状态
                    // -----------------------------------------

                    if (task.IsComplete)
                    {
                        GUILayout.Label(
                            "✔ Complete",
                            GUILayout.Width(100)
                        );
                    }
                    else if (
                        player ==
                        PlayerControl.LocalPlayer)
                    {
                        if (GUILayout.Button(
                                "Complete",
                                GUILayout.Width(100)
                            ))
                        {
                            CompleteTask(task);
                        }
                    }


                    GUILayout.EndHorizontal();
                }
            }


            GUILayout.EndVertical();
        }


        // =====================================================
        // 判断是否为破坏任务
        // =====================================================

        private bool IsSabotageTask(PlayerTask task)
        {
            if (task == null)
                return false;

            return task.TaskType is
                TaskTypes.ResetReactor or
                TaskTypes.RestoreOxy or
                TaskTypes.FixLights or
                TaskTypes.FixComms or
                TaskTypes.ResetSeismic or
                TaskTypes.StopCharles or
                TaskTypes.MushroomMixupSabotage;
        }


        // =====================================================
        // 完成单个任务
        // =====================================================

        private void CompleteTask(PlayerTask task)
        {
            if (task == null)
                return;

            try
            {
                // -------------------------------------------------
                // PlayerTask.IsComplete 是只读属性
                // 因此只能使用游戏提供的 Complete()
                // -------------------------------------------------

                task.Complete();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ShowTaskMenu] Failed to complete task: "
                    + ex
                );
            }
        }


        // =====================================================
        // 完成自己的所有任务
        // =====================================================

        private void CompleteMyTasks()
        {
            var local =
                PlayerControl.LocalPlayer;

            if (local == null)
                return;

            if (local.myTasks == null)
                return;


            foreach (var task in local.myTasks)
            {
                if (task == null)
                    continue;

                if (task.IsComplete)
                    continue;

                if (IsSabotageTask(task))
                    continue;

                CompleteTask(task);
            }
        }
    }
}