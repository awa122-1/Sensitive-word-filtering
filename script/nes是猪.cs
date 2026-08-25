using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AmongUsFilterMod.Utils
{
    public static class ResourceLoader
    {
        /// <summary>
        /// 加载图片为 Sprite
        /// </summary>
        public static Sprite LoadSprite(string fileName, float pixelsPerUnit = 100f)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = null;

                // 自动匹配 DLL 内部嵌入资源全路径
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = name;
                        break;
                    }
                }

                // 1. 从 Embedded Resource 集中读取
                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, bytes.Length);

                            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                            {
                                filterMode = FilterMode.Point,
                                wrapMode = TextureWrapMode.Clamp
                            };

                            if (ImageConversion.LoadImage(texture, bytes))
                            {
                                return Sprite.Create(
                                    texture,
                                    new Rect(0, 0, texture.width, texture.height),
                                    new Vector2(0.5f, 0.5f),
                                    pixelsPerUnit
                                );
                            }
                        }
                    }
                }

                // 2. 降级方案：从 BepInEx/plugins 本地磁盘路径读取
                string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (File.Exists(diskPath))
                {
                    byte[] diskBytes = File.ReadAllBytes(diskPath);
                    Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };

                    if (ImageConversion.LoadImage(diskTex, diskBytes))
                    {
                        return Sprite.Create(
                            diskTex,
                            new Rect(0, 0, diskTex.width, diskTex.height),
                            new Vector2(0.5f, 0.5f),
                            pixelsPerUnit
                        );
                    }
                }

                Debug.LogWarning($"[ResourceLoader] ⚠️ 未在嵌入资源或磁盘中查找到图片: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceLoader] LoadSprite 出现异常: {ex}");
            }

            return null;
        }
    }
}