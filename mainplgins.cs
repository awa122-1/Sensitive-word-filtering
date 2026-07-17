using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AmongUsFilterMod
{
    // 1. 回归官方标准声明，直接使用 BepInPlugin，不再依赖自动生成
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    public class MyPlugin : BasePlugin
    {
        // 2. 模组基本信息定义
        public const string Id = "com.paul.amongus.filtermod";
        public const string Name = "AU_WordFilterMod";
        public const string Version = "1.0.0";

        // 3. 使用 new 关键字显式消除成员隐藏警告
        public static new ManualLogSource Log;
        public static NewDfaFilter FilterInstance;
        private Harmony _harmony;

        public override void Load()
        {
            // 绑定基类的 Log
            Log = base.Log; 
            Log.LogInfo("Among Us 拼音/粗话过滤模组正在初始化...");

            // 4. 注册编码提供器（支持 GB2312 拼音转换）
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"注册编码提供器失败: {ex.Message}");
            }

            // 5. 初始化高防 DFA 拼音检测器
            FilterInstance = new NewDfaFilter();

            // 6. 异步加载词库
            string pluginFolder = Path.GetDirectoryName(this.GetType().Assembly.Location);
            _ = LoadWordsAsync(pluginFolder);

            // 7. 挂载 Harmony 拦截补丁
            try
            {
                _harmony = new Harmony(Id);
                _harmony.PatchAll();
                Log.LogInfo("Harmony 拦截补丁已成功挂载！");
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony 补丁挂载失败: {ex.Message}");
            }

            Log.LogInfo("Among Us 拼音/粗话过滤模组加载完成！");
        }

        private async Task LoadWordsAsync(string pluginFolder)
        {
            if (string.IsNullOrEmpty(pluginFolder)) return;

            string folderPath = Path.Combine(pluginFolder, "FilterWords");
            
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    string defaultFile = Path.Combine(folderPath, "chinese.txt");
                    
                    await File.WriteAllLinesAsync(defaultFile, new string[] 
                    { 
                        "# 在这里一行输入一个敏感词，支持中文/英文/拼音",
                        "# 模组会自动提取拼音和首字母缩写",
                        "傻逼",
                        "草泥马",
                        "垃圾",
                        "fuck",
                        "wdnmd"
                    });
                    Log.LogInfo("未检测到词库，已在 plugins 目录下生成默认 FilterWords 文件夹及模板！");
                }

                int totalLoaded = 0;
                foreach (string file in Directory.GetFiles(folderPath, "*.txt"))
                {
                    string[] lines = await File.ReadAllLinesAsync(file);
                    foreach (string line in lines)
                    {
                        string cleanLine = line.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanLine) && !cleanLine.StartsWith("#"))
                        {
                            FilterInstance.AddWord(cleanLine);
                            totalLoaded++;
                        }
                    }
                }
                Log.LogInfo($"成功加载本地词库！共导入了 {totalLoaded} 个核心敏感词。");
            }
            catch (Exception ex)
            {
                Log.LogError($"加载本地词库文件时发生异常: {ex.Message}");
            }
        }

        public override bool Unload()
        {
            _harmony?.UnpatchSelf();
            return true;
        }
    }
}