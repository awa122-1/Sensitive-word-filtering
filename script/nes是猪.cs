using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AmongUsFilterMod.Utils
{
    public static class ResourceLoader
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        // 注意：根据你的 VS 项目默认命名空间修改，规则为: "项目默认命名空间.文件夹名."
        private const string EmbeddedResourcePrefix = "AmongUsFilterMod.Resources.";

        /// <summary>
        /// 注册并获取一个 Sprite 贴图
        /// </summary>
        public static Sprite GetSprite(string fileName, float pixelsPerUnit = 100f)
        {
            if (SpriteCache.TryGetValue(fileName, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            Texture2D texture = GetTexture(fileName);
            if (texture == null) return null;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            
            // 阻止 Unity 在 GC 时误卸载该资源
            UnityEngine.Object.DontDestroyOnLoad(texture);
            UnityEngine.Object.DontDestroyOnLoad(sprite);

            SpriteCache[fileName] = sprite;
            return sprite;
        }

        /// <summary>
        /// 注册并获取一个 Texture2D 贴图
        /// </summary>
        public static Texture2D GetTexture(string fileName)
        {
            if (TextureCache.TryGetValue(fileName, out Texture2D cachedTex) && cachedTex != null)
            {
                return cachedTex;
            }

            byte[] data = ReadResourceBytes(fileName);
            if (data == null || data.Length == 0)
            {
                Debug.LogError($"[ResourceLoader] 无法读取图片二进制数据: {fileName}");
                return null;
            }

            // 必须支持 Read/Write，使用 RGBA32 格式
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            if (ImageConversion.LoadImage(texture, data))
            {
                TextureCache[fileName] = texture;
                return texture;
            }

            Debug.LogError($"[ResourceLoader] 图片转换 Texture2D 失败: {fileName}");
            return null;
        }

        /// <summary>
        /// 资源读取逻辑（双轨制：优先从 DLL 嵌入资源读取，读不到则从本地磁盘插件目录读取）
        /// </summary>
        private static byte[] ReadResourceBytes(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourcePath = EmbeddedResourcePrefix + fileName;

            // 方式 A：DLL 内嵌资源
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }

            // 方式 B：本地磁盘插件目录 (BepInEx/plugins/fileName)
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(diskPath))
            {
                return File.ReadAllBytes(diskPath);
            }

            return null;
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
            TextureCache.Clear();
        }
    }
}